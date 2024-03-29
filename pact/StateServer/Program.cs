using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace StateServer
{
	class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWebApplication()
                .Build();

            host.Run();
            await Task.CompletedTask;
        }
    }
}