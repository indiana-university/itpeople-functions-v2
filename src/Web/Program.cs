using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;

namespace web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddHttpClient("Api", client => {
                // Get the BaseAddress from our configuration
                client.BaseAddress = new Uri(Utils.Env(builder.Configuration, "API_URL", true));
                
                // Attempt to fetch the JWT from storage
                // Get user data from the Local Storage Service
                var sp = builder.Services.BuildServiceProvider();
                var localStorage = sp.GetService<ISyncLocalStorageService>();

                var user = localStorage.GetItem<AuthenticatedUser>("user");
                var jwtString = user?.AccessToken ?? "";

                // If we got a JWT use it to add an Authorization header for API requestes.
                if(string.IsNullOrWhiteSpace(jwtString) == false)
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"bearer {jwtString}");
                }
                else
                {
                    throw new Exception("Could not find an access token in Local Storage, is the user currently logged in?");
                }
            });

            await builder.Build().RunAsync();
        }
    }
}
