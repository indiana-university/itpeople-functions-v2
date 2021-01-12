using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Docker.DotNet;

namespace Integration
{
    [SetUpFixture]
    public class Harness
    {
        private readonly IDockerClient _client;
        private readonly SqlServerContainer _dbContainer;

        public Harness()
        {
            var uri  = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new Uri("npipe://./pipe/docker_engine") 
                : new Uri("unix:/var/run/docker.sock");
            _client = new DockerClientConfiguration(uri).CreateClient();
            _dbContainer = new SqlServerContainer(TestContext.Progress);
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {

            // Start SQL Server container
            if (System.Environment.GetEnvironmentVariable("CI_BUILD") != "true")
            {
                TestContext.Progress.WriteLine("🔍 CI_BUILD environment variable is not set (or not set to 'true'). Will attempt to start the print_integration_test container...");
                _dbContainer.Start(_client).Wait(60*1000);
            }
            else
            {
                TestContext.Progress.WriteLine("🔍 CI_BUILD environment variable set to 'true'. Assuming that the print_integration_test container was started by CI platform.");
            }

            // Wait for SQL Server container to finish starting
            _dbContainer.WaitUntilReady().Wait(60*1000);

            // Build and start API Function app container
            // Wait for API container to finish starting
        }

        [OneTimeTearDown]
        public void OneTimeTeardown()
        {
            // Stop and delete API container
            // Stop (but don't delete) the SQL Server container
        }
    }
}