using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Tasks
{
	class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .Build();

            host.Run();
            await Task.CompletedTask;
        }
    }
}