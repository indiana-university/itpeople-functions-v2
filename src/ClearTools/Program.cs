using Microsoft.Extensions.Configuration;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Controls;

namespace ClearTools;

class Program
{
    const string SearchBase = "ou=Accounts,dc=ads,dc=iu,dc=edu";
    const string SamAccountName = "sAMAccountName";
    const int PageSize = 500;

    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();

        var adsUser = $"ads\\{config["AdToolsGroupManagerUser"] ?? throw new Exception("Missing AdToolsGroupManagerUser")}";
        var adsPassword = config["AdToolsGroupManagerPassword"] ?? throw new Exception("Missing AdToolsGroupManagerPassword");
        var ldapPaths = config.GetSection("LdapGroupPaths").Get<string[]>() ?? throw new Exception("Missing LdapGroupPaths");

        using var ldap = new LdapConnection { SecureSocketLayer = true };
        ldap.Connect("ads.iu.edu", 636);
        ldap.Bind(adsUser, adsPassword);

        foreach (var groupDn in ldapPaths)
        {
            var cn = ParseDnComponent(groupDn, "cn");
            var firstOu = ParseDnComponent(groupDn, "ou");
            var csvPath = Path.Combine(firstOu, $"{cn}.csv");

            Console.Write($"Reading members of {firstOu}/{cn} from LDAP...");
            var members = GetGroupMembers(ldap, groupDn);
            Console.WriteLine($" {members.Count} found.");

            if (File.Exists(csvPath))
            {
                var existing = (await File.ReadAllLinesAsync(csvPath)).Skip(1).Count(l => !string.IsNullOrWhiteSpace(l));
                Console.WriteLine($"  CSV already exists with {existing} entries. Overwrite? [Y/N]");
                var response = Console.ReadLine()?.Trim();
                if (!string.Equals(response, "Y", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("  Skipped.");
                    continue;
                }
            }

            Directory.CreateDirectory(firstOu);
            await File.WriteAllLinesAsync(csvPath, new[] { SamAccountName }.Concat(members));
            Console.WriteLine($"  Written to {csvPath}");
        }
    }

    static string ParseDnComponent(string dn, string type)
    {
        var prefix = $"{type}=";
        foreach (var part in dn.Split(','))
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return trimmed[prefix.Length..];
        }
        throw new Exception($"No {type}= component found in: {dn}");
    }

    static List<string> GetGroupMembers(LdapConnection ldap, string groupDn)
    {
        var members = new List<string>();
        var page = 0;
        var hasMore = false;

        do
        {
            var controls = new LdapControl[]
            {
                new LdapVirtualListControl(page * PageSize + 1, 0, PageSize - 1, 0),
                new LdapSortControl(new LdapSortKey("cn"), true)
            };
            var constraints = new LdapSearchConstraints();
            constraints.SetControls(controls);

            var search = ldap.Search(SearchBase, LdapConnection.ScopeSub, $"(memberOf={groupDn})", new[] { SamAccountName }, false, constraints);
            hasMore = false;

            while (search.HasMore())
            {
                var netid = search.Next().GetAttribute(SamAccountName).StringValue;
                if (members.Contains(netid))
                    break;
                members.Add(netid);
                hasMore = true;
            }

            page++;
        } while (hasMore);

        return members.OrderBy(m => m).ToList();
    }
}
