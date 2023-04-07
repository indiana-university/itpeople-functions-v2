using System.Threading.Tasks;
using Docker.DotNet.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using Database;

namespace Integration
{
    public class FunctionAppContainerBase : DockerContainer
    {

        public FunctionAppContainerBase(TextWriter progress, TextWriter error, string imageName, string containerName, string dockerFile, int port) 
            : base(progress, error, imageName, containerName)
        {
            DockerFile = dockerFile;
            Port = port;
        }

        public void BuildImage()
        {
            Progress.WriteLine($"⏳ Building Function App image '{ImageName}'. This can take some time -- hang in there!");
            DockerExec($"build --pull --rm --file {DockerFile} --tag {ImageName} .", "../../../../../../");
        }


        private static System.Net.Http.HttpClient http = new System.Net.Http.HttpClient();

        public string DockerFile { get; }
        public int Port { get; }

        protected override async Task<bool> isReady()
        {
            try
            {
                var response = await http.GetAsync($"http://localhost:{Port}/ping");
                // look for HTTP OK (200) response
                if(response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
                else
                {
                    throw new Exception($"Status Code: {response.StatusCode}");
                }
            }
            catch (Exception e)
            {
                Progress.WriteLine($"🤔 {ContainerName} is not yet ready: {e.Message}");
                return false;
            }
        }

        public override Config ToConfig() 
            => new Config
                {
                    Env = new List<string> 
                    { 
                        // Add test-only environment variables here, or
                        // add them in Dockerfile.API
                    }
                };

        public override HostConfig ToHostConfig()
            => new HostConfig()
                {
                    NetworkMode = NetworkName,
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            {
                                "80/tcp",
                                new List<PortBinding>
                                {
                                    new PortBinding
                                    {
                                        HostPort = Port.ToString(),
                                    }
                                }
                            },
                        },
                };

    }
    public class FunctionAppContainer : FunctionAppContainerBase
    {
        public FunctionAppContainer(TextWriter progress, TextWriter error) 
            : base(progress, error, "integration-test-api:dev", $"integration-test-api", "Dockerfile.API2", 8080)
        {
        }
    }
}