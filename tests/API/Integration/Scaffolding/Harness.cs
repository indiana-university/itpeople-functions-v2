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
        public static PostgresContainer DbContainer { get; private set; }
        public static FunctionAppContainer AppContainer {get; private set; }
        public static StateServerContainer StateContainer {get; private set; }

        public Harness()
        {
            var uri  = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new Uri("npipe://./pipe/docker_engine") 
                : new Uri("unix:/var/run/docker.sock");
            _client = new DockerClientConfiguration(uri).CreateClient();
            DbContainer = new PostgresContainer(TestContext.Progress, TestContext.Error);
            AppContainer = new FunctionAppContainer(TestContext.Progress, TestContext.Error);
            StateContainer = new StateServerContainer(TestContext.Progress, TestContext.Error);
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            EnsureIntegrationTestsNetworkExists();

            // Start SQL Server container
            try { DbContainer.Remove(_client).Wait(60*1000); } catch {}
            DbContainer.Pull();
            DbContainer.Start(_client).Wait(60*1000);
            // Wait for SQL Server container to finish starting
            DbContainer.WaitUntilReady().Wait(60*1000);

            try { AppContainer.Remove(_client).Wait(60*1000); } catch {}
            // Build and start API Function app container
            AppContainer.BuildImage();
            AppContainer.Start(_client).Wait(10 * 1000);
            // Wait for API container to finish starting
            AppContainer.WaitUntilReady().Wait(10 * 1000);

            try { StateContainer.Remove(_client).Wait(60*1000); } catch {}
            // Build and start API Function app container
            StateContainer.BuildImage();
            StateContainer.Start(_client).Wait(10 * 1000);
            // Wait for API container to finish starting
            StateContainer.WaitUntilReady().Wait(10 * 1000);
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
            // AppContainer.Remove(_client).Wait(60*1000);
            // DbContainer.Remove(_client).Wait(60*1000);
        }
    }
}