using System.Threading.Tasks;
using System;
using System.IO;
using System.Collections.Generic;
using Database;
using Microsoft.EntityFrameworkCore;
using Models;
using System.Data.Common;

namespace Integration
{
    public abstract class DatabaseContainer : DockerContainer
    {
        protected DatabaseContainer(TextWriter progress, TextWriter error, string imageName) 
            : base(progress, error, imageName, "integration-test-db")
        {
        }

        public static string ConnectionString { get; private set; }

        // Gotta wait until the database server is really available
        // or you'll get oddball test failures;)
        protected override async Task<bool> isReady()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    await conn.OpenAsync();
                    return true;
                }
            }
            catch (Exception e)
            {
                Progress.WriteLine($"🤔 {ContainerName} is not yet ready: {e.Message}");
                return false;
            }
        }

        protected abstract DbConnection GetConnection();
    }
}