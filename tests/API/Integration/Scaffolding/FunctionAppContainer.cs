using System.Threading.Tasks;
using Docker.DotNet.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using Database;

namespace Integration
{
    internal class FunctionAppContainer : DockerContainer
    {
        public FunctionAppContainer(TextWriter progress, TextWriter error) 
            : base(progress, error, "integration-test-api:dev", $"integration-test-api")
        {
        }

        public void BuildImage() 
        {
            Progress.WriteLine($"⏳ Building Function App image '{ImageName}'. This can take some time -- hang in there!");
            var p = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "docker",
                    Arguments = $"build --pull --rm --file Dockerfile.API --tag {ImageName} .",
                    WorkingDirectory = "../../../../../../", // repo root 🤞🏻
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,                    
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
            };
            p.Start();            
            p.WaitForExit();

            var output = p.StandardOutput.ReadToEnd();
            var error = p.StandardError.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(output))
                Progress.WriteLine(output);
            if (!string.IsNullOrWhiteSpace(error))
                Error.WriteLine(output);

            if (p.ExitCode == 0)
            {
                Progress.WriteLine($"😎 Finished building '{ImageName}'.");
            }
            else
            {
                Error.WriteLine($"🤮 Failed to build '{ImageName}'.");
            }
        }

        protected override async Task<bool> isReady()
        {
            try
            {
                // send http request to localhost:8080/api/ping
                var client = new System.Net.Http.HttpClient();
                var response = await client.GetAsync("http://localhost:8080/api/ping");
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
                        // $"SqlServerConnectionString='{PeopleContext.LocalConnectionString}'", 
                        // $"JwtPublicKey=-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAnzyis1ZjfNB0bBgKFMSv\nvkTtwlvBsaJq7S5wA+kzeVOVpVWwkWdVha4s38XM/pa/yr47av7+z3VTmvDRyAHc\naT92whREFpLv9cj5lTeJSibyr/Mrm/YtjCZVWgaOYIhwrXwKLqPr/11inWsAkfIy\ntvHWTxZYEcXLgAXFuUuaS3uF9gEiNQwzGTU1v0FqkqTBr4B8nW3HCN47XUu0t8Y0\ne+lf4s4OxQawWD79J9/5d3Ry0vbV3Am1FtGJiJvOwRsIfVChDpYStTcHTCMqtvWb\nV6L11BWkpzGXSW4Hv43qa+GSYOD2QU68Mb59oSk2OB+BtOLpJofmbGEGgvmwyCI9\nMwIDAQAB\n-----END PUBLIC KEY-----"
                    }
                };

        public override HostConfig ToHostConfig()
            => new HostConfig()
                {
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                        {
                            {
                                "80/tcp",
                                new List<PortBinding>
                                {
                                    new PortBinding
                                    {
                                        HostPort = $"8080",
                                        HostIP = "localhost"
                                    }
                                }
                            },
                        },
                };
    }
}