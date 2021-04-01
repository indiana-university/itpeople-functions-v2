using Docker.DotNet.Models;
using System.IO;
using System.Collections.Generic;
using Database;
using Npgsql;
using System.Data.Common;

namespace Integration
{
    public class PostgresContainer : DatabaseContainer
    {
        public PostgresContainer(TextWriter progress, TextWriter error) 
            : base(progress, error, "postgres:11.7-alpine")
        {
        }

        public void Pull()
        {
            DockerExec($"pull {ImageName}", ".");
        }

        public override Config ToConfig() 
            => new Config
            {
                Env = new List<string> 
                { 
                    "POSTGRES_USER=SA",
                    "POSTGRES_PASSWORD=abcd1234@",
                }
            };

        // Watch the port mapping here to avoid port
        // contention w/ other Sql Server installations
        public override HostConfig ToHostConfig() 
            => new HostConfig()
            {
                NetworkMode = NetworkName,
                PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        {
                            "5432/tcp",
                            new List<PortBinding>
                            {
                                new PortBinding
                                {
                                    HostPort = $"5432"
                                }
                            }
                        },
                    },
            };

        protected override DbConnection GetConnection() 
            => new NpgsqlConnection(PeopleContext.LocalServerConnectionString);
    }
}