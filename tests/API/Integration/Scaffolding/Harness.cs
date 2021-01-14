using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Docker.DotNet;
using System.Linq;
using Docker.DotNet.Models;

namespace Integration
{
    [SetUpFixture]
    public class Harness
    {
        private const string Network = "IntegrationTests";
        private readonly IDockerClient _client;
        private readonly SqlServerContainer _dbContainer;
        private readonly FunctionAppContainer _appContainer;

        public Harness()
        {
            var uri  = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new Uri("npipe://./pipe/docker_engine") 
                : new Uri("unix:/var/run/docker.sock");
            _client = new DockerClientConfiguration(uri).CreateClient();
            _dbContainer = new SqlServerContainer(TestContext.Progress, TestContext.Error);
            _appContainer = new FunctionAppContainer(TestContext.Progress, TestContext.Error);
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            EnsureIntegrationTestsNetworkExists();

            // Start SQL Server container
            _dbContainer.Start(_client).Wait(60 * 1000);
            // Wait for SQL Server container to finish starting
            _dbContainer.WaitUntilReady().Wait(60 * 1000);

            // Build and start API Function app container
            _appContainer.BuildImage();
            _appContainer.Start(_client).Wait(10 * 1000);
            // Wait for API container to finish starting
            _appContainer.WaitUntilReady().Wait(10 * 1000);

        }

        private void EnsureIntegrationTestsNetworkExists()
        {
            var networks = _client.Networks.ListNetworksAsync().Result;
            if (!networks.Any(n => n.Name == DockerContainer.NetworkName))
            {
                _client.Networks
                    .CreateNetworkAsync(new NetworksCreateParameters() { Name = DockerContainer.NetworkName })
                    .Wait();
            }
        }

        [OneTimeTearDown]
        public void OneTimeTeardown()
        {
            _appContainer.Stop(_client).Wait(60*1000);
            _appContainer.Remove(_client).Wait(60*1000);
            // _dbContainer.Stop(_client).Wait(60 * 1000);
        }
    }
}