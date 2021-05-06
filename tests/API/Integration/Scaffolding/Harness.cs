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
            DbContainer.Start(_client).Wait();
            // Wait for SQL Server container to finish starting
            DbContainer.WaitUntilReady().Wait();

            try { AppContainer.Remove(_client).Wait(60*1000); } catch {}
            // Build and start API Function app container
            AppContainer.BuildImage();
            AppContainer.Start(_client).Wait();
            // Wait for API container to finish starting
            AppContainer.WaitUntilReady().Wait();

            try { StateContainer.Remove(_client).Wait(60*1000); } catch {}
            // Build and start API Function app container
            StateContainer.BuildImage();
            StateContainer.Start(_client).Wait();
            // Wait for API container to finish starting
            StateContainer.WaitUntilReady().Wait();
        }

        private void EnsureIntegrationTestsNetworkExists()
        {
            var networks = _client.Networks.ListNetworksAsync().Result;
            if (!networks.Any(n => n.Name == DockerContainer.NetworkName))
            {
                TestContext.Progress.WriteLine($"⏳ Creating test network '{DockerContainer.NetworkName}'...");
                _client.Networks
                    .CreateNetworkAsync(new NetworksCreateParameters() { Name = DockerContainer.NetworkName })
                    .Wait();
            }
            else
            {
                TestContext.Progress.WriteLine($"😎 Test network '{DockerContainer.NetworkName}' exists.");
            }
            TestContext.Progress.WriteLine($"🔍 Docker Networks (name, driver, scope):");
            foreach (var network in _client.Networks.ListNetworksAsync().Result)
            {
                TestContext.Progress.WriteLine($"  {network.Name}, {network.Driver}, {network.Scope}");
            }

        }

        [OneTimeTearDown]
        public void OneTimeTeardown()
        {
            AppContainer.Stop(_client).Wait(60*1000);
            DbContainer.Stop(_client).Wait(60*1000);
            StateContainer.Stop(_client).Wait(60*1000);
        }
    }
}