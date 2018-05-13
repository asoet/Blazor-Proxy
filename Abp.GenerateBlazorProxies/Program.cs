using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System;
using Microsoft.Extensions.DependencyInjection;
using Abp.Web.Api.Modeling;
using System.Linq;
using System.IO;
using //path to project abp server project

namespace Abp.GenerateBlazorProxies
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("Generating Proxies");
            var testServer = new TestServer(new WebHostBuilder()
            .UseStartup<Startup>());
            var apiModel = testServer.Host.Services.GetService<IApiDescriptionModelProvider>();
            if (apiModel == null)
            {
                Console.WriteLine("ABP not propper initialized. IApiDescriptionModelProvider not in DI.");
                return;
            }
            var model = apiModel.CreateModel();
            var blazorGenerator = new BlazorProxyScriptGenerator();
            var script = blazorGenerator.CreateScript(model);
            Console.WriteLine("Script generated");
            var proxyFileNameLocation = Environment.CurrentDirectory;
            if (args.Any())
            {
                proxyFileNameLocation = Path.GetFullPath(args[0]);
            }
            Console.WriteLine("Path found");
            var proxyFile = proxyFileNameLocation+ "/Proxies.cs";
            Console.WriteLine($"FileName {proxyFile}");
            await File.WriteAllTextAsync(proxyFile, script);
            Console.WriteLine("Proxy file written");
            File.Copy(Path.Combine(Environment.CurrentDirectory, "ABPResult.cs"), Path.Combine(proxyFileNameLocation, "ABPResult.cs"));
            Console.WriteLine("ABPResult file written");
        }
    }
}
