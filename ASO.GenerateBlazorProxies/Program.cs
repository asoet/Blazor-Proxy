using Microsoft.AspNetCore.Hosting;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using ASO.GenerateBlazorProxies.MVC;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;
using TwillioCallFlow.Server;

namespace ASO.GenerateBlazorProxies
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("Generating Proxies");
            await GenerateMVC(args);
        }

        public static async Task GenerateMVC(string[] args)
        {
            var testServer = new TestServer(new WebHostBuilder().ConfigureServices(services => {
                services.AddSingleton<IApiDescriptionGroupCollectionProvider, ApiDescriptionGroupCollectionProvider>();
                services.TryAddEnumerable(
                    ServiceDescriptor.Transient<IApiDescriptionProvider, DefaultApiDescriptionProvider>());

            })
            .UseStartup<Startup>());
            var apiModel = testServer.Host.Services.GetService<IApiDescriptionGroupCollectionProvider>();
            if (apiModel == null)
            {
                return;
            }
            var blazorGenerator = new MVCBlazorProxyScriptGenerator();
            var script = blazorGenerator.CreateScript(apiModel);
            Console.WriteLine("Script generated");
            var proxyFileNameLocation = Environment.CurrentDirectory;
            if (args.Any())
            {
                proxyFileNameLocation = Path.GetFullPath(args[0]);
            }
            Console.WriteLine("Path found");
            var proxyFile = proxyFileNameLocation + "/Proxies.cs";
            Console.WriteLine($"FileName {proxyFile}");
            await File.WriteAllTextAsync(proxyFile, script.script);
            Console.WriteLine("Proxy file written");
            if (script.isABPResultAdded)
                AddABPResult(args);
        }

        public static void AddABPResult(string[] args)
        {
            var proxyFileNameLocation = Environment.CurrentDirectory;
            if (args.Any())
            {
                proxyFileNameLocation = Path.GetFullPath(args[0]);
            }
            var newAbpResultPath = Path.Combine(proxyFileNameLocation, "ABPResult.cs");
            if (!File.Exists(newAbpResultPath))
                File.Copy(Path.Combine(Environment.CurrentDirectory, "ABPResult.cs"), newAbpResultPath);
            Console.WriteLine("ABPResult file written");
        }
    }
}
