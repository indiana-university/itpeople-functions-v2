using System.Data.SqlClient;
using Docker.DotNet.Models;
using System.IO;
using System.Collections.Generic;
using Database;
using System.Data.Common;

namespace Integration
{

    ///<summary>See https://hub.docker.com/_/microsoft-mssql-server for imags, tags, and usage notes.</summary>
    public class SqlServerContainer : DatabaseContainer
    {        
        public SqlServerContainer(TextWriter progress, TextWriter error) 
            : base(progress, error, "mcr.microsoft.com/mssql/server:2019-latest")
        {
        }


        // Watch the port mapping here to avoid port
        // contention w/ other Sql Server installations
        public override HostConfig ToHostConfig() 
            => new HostConfig()
            {
                NetworkMode = NetworkName,
                PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        {
                            "1433/tcp",
                            new List<PortBinding>
                            {
                                new PortBinding
                                {
                                    HostPort = $"1433"
                                }
                            }
                        },
                    },
            };

        public override Config ToConfig() 
            => new Config
            {
                Env = new List<string> { "ACCEPT_EULA=Y", "SA_PASSWORD=abcd1234@", "MSSQL_PID=Developer" }
            };

        protected override DbConnection GetConnection() 
            => new SqlConnection(PeopleContext.LocalServerConnectionString);
    }
}