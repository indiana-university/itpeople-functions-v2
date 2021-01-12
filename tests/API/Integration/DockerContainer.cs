using Docker.DotNet;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Integration
{
    internal abstract class DockerContainer
    {
        public string ImageName { get; }
        public string ContainerName { get; }
        public TextWriter Progress { get; }
        public TextWriter Error { get; }
        // public StartAction StartAction { get; private set; } = StartAction.none;

        protected DockerContainer(TextWriter progress, TextWriter error, string imageName, string containerName)
        {
            Progress = progress;
            Error = error;
			ImageName = imageName;
            ContainerName = containerName; // + "-" + Guid.NewGuid().ToString();
        }

        public async Task Start(IDockerClient client)
        {
            // if (StartAction != StartAction.none) return;

            var images =
                await client.Images.ListImagesAsync(new ImagesListParameters { MatchName = ImageName });

            if (images.Count == 0)
            {
                Progress.WriteLine($"⏳ Fetching Docker image '{ImageName}'. This can take a long time -- hang in there!");

                await client.Images.CreateImageAsync(
                    new ImagesCreateParameters { FromImage = ImageName }, null, new ConsoleProgress(Progress));
            }

            var list = await client.Containers.ListContainersAsync(new ContainersListParameters
            {
                All = true
            });

            var container = list.FirstOrDefault(x => x.Names.Contains("/" + ContainerName));
            if (container == null)
            {
                await createContainer(client);

            }
            else
            {
                if (container.State == "running")
                {
                    Progress.WriteLine($"😎 Container '{ContainerName}' is already running.");
                    // StartAction = StartAction.external;
                    return;
                }
            }

            var started = await client.Containers.StartContainerAsync(ContainerName, new ContainerStartParameters());
            if (!started)
            {
                throw new InvalidOperationException($"😫 Container '{ContainerName}' did not start!!!!");
            }
            // StartAction = StartAction.started;
        }

        public async Task WaitUntilReady()
        {
            Progress.WriteLine($"⏳ Waiting for container '{ContainerName}' to become ready...");
            var i = 0;
            do
            {
                if (i++ > 30)
                {
                    throw new TimeoutException($"😫 Container {ContainerName} does not seem to be responding in a timely manner");
                }
                await Task.Delay(2000);
            }
            while(!await isReady());

            Progress.WriteLine($"😎 Container '{ContainerName}' is ready.");
        }

        private async Task createContainer(IDockerClient client)
        {
            Progress.WriteLine($"⏳ Creating container '{ContainerName}' using image '{ImageName}'");
            await client.Containers.CreateContainerAsync(new CreateContainerParameters(ToConfig())
            {
                Image = ImageName,
                Name = ContainerName,
                Tty = true,
                HostConfig = ToHostConfig(),
            });
        }

        public async Task Stop(IDockerClient client)
        {            
            try
            {
                Progress.WriteLine($"⏳ Stopping '{ContainerName}'....");
                await client.Containers.StopContainerAsync(ContainerName, new ContainerStopParameters());
            }
            catch(Exception ex)
            {
                Error.WriteLine($"❌Error encountered while stopping {ContainerName}: {ex.Message}");
            }
        }

        public Task Remove(IDockerClient client)
        {
            return client.Containers.RemoveContainerAsync(ContainerName,
                new ContainerRemoveParameters { Force = true });
        }

        protected abstract Task<bool> isReady();

        public abstract HostConfig ToHostConfig();

        public abstract Config ToConfig();

        public override string ToString()
        {
            return $"{nameof(ImageName)}: {ImageName}, {nameof(ContainerName)}: {ContainerName}";
        }
    }

    public class ConsoleProgress : IProgress<JSONMessage>
    {
        private readonly TextWriter _progress;

        public ConsoleProgress(TextWriter progress)
        {
            _progress = progress;
        }

        public void Report(JSONMessage value)
        {
            _progress.WriteLine(value.ProgressMessage);
        }
    }
}
