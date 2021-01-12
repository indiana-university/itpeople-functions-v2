using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Docker.DotNet;

namespace Integration
{
    [SetUpFixture]
    public class Harness
    {
        private readonly bool _isCIBuild; 
        private readonly IDockerClient _client;
        private readonly SqlServerContainer _dbContainer;
        private readonly FunctionAppContainer _appContainer;

        public Harness()
        {
            _isCIBuild = System.Environment.GetEnvironmentVariable("CI_BUILD") == "true";
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
            // Start SQL Server container
            /*
            if (_isCIBuild)
            {
                TestContext.Progress.WriteLine("🔍 CI_BUILD environment variable set to 'true'. Assuming that the database container was started by CI platform.");
            }
            else
            {
                TestContext.Progress.WriteLine("🔍 CI_BUILD environment variable is not set (or not set to 'true'). Will attempt to start the database container...");
                _dbContainer.Start(_client).Wait(60 * 1000);
            }
            */
            // Wait for SQL Server container to finish starting
            _dbContainer.Start(_client).Wait(60 * 1000);
            _dbContainer.WaitUntilReady().Wait(60*1000);

            _appContainer.BuildImage();
            _appContainer.Start(_client).Wait(60*1000);
            _appContainer.WaitUntilReady().Wait(60*1000);

            // Build and start API Function app container
            // Wait for API container to finish starting
        }

        [OneTimeTearDown]
        public void OneTimeTeardown()
        {
            _appContainer.Stop(_client).Wait(60*1000);
            _dbContainer.Stop(_client).Wait(60 * 1000);
/*
            if (_isCIBuild)
            {
                TestContext.Progress.WriteLine("🔍 CI_BUILD environment variable set to 'true'. Assuming that the database container will be stopped by CI platform.");
            }
            else
            {
                TestContext.Progress.WriteLine("🔍 CI_BUILD environment variable is not set (or not set to 'true'). Will attempt to stop the database container...");
                _dbContainer.Stop(_client).Wait(60 * 1000);
            }
*/
        }
    }
}