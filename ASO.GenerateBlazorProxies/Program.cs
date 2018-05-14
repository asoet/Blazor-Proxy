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
using ASO.GenerateBlazorProxies.Helpers;
using System.Collections.Generic;
using NDesk.Options;

namespace ASO.GenerateBlazorProxies
{
    class Program
    {
		private static string TargetAssembly { get; set; }
		private static string TargetBasePathForFiles { get; set; }
		private static string OutputDir { get; set; }
		private static string OutputAssembly { get; set; }
		private static string NamespacesJoined { get; set; }

        static async System.Threading.Tasks.Task Main(string[] args)
        {
			bool show_help = false;

			var p = new OptionSet() {
				{ "ta|targetassembly=", "target assembly path. In this assembly is the startup file.",
					v => TargetAssembly = v },
				{ "tbp|targetbasepath=", "target base path. this path contains all the required return types to copy with the proxies.",
					v => TargetBasePathForFiles = v },
                { "od|outputdir=", "this is the dir to copy the proxies and return types to.",
					v => OutputDir = v },
				{ "oa|outputassembly=", "this is the assembly to check if return types exists.",
                    v => OutputAssembly = v },
                { "n|namespaces=", "the namespaces of the return types. seperate with ','",
                    v => NamespacesJoined = v },
                { "h|help",  "show this message and exit",
                   v => show_help = v != null }
            };

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("Proxies: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `--help' for more information.");
                return;
            }
			await GenerateMVC();
        }
      
        public static async Task GenerateMVC()
        {
			var assembly = AssemblyLoader.LoadFromAssemblyPath(TargetAssembly);
			var all = assembly.GetTypes();
			var startUpType = assembly.GetTypes().FirstOrDefault(f=>f.Name == "Startup");
			if (startUpType == null)
				return;
			try
			{
				var testServer = new TestServer(new WebHostBuilder()
                                            .UseStartup(startUpType));
                var apiModel = testServer.Host.Services.GetService<IApiDescriptionGroupCollectionProvider>();
                if (apiModel == null)
                {
                    return;
                }
                var blazorGenerator = new MVCBlazorProxyScriptGenerator();
                var script = blazorGenerator.CreateScript(apiModel);
                
                Console.WriteLine("Script generated");
                var proxyFileNameLocation = Environment.CurrentDirectory;
				if (!string.IsNullOrEmpty(OutputDir))
                {
					proxyFileNameLocation = Path.GetFullPath(OutputDir);
                }
                Console.WriteLine("Path found");
                var proxyFile = proxyFileNameLocation + "/Proxies.cs";
                Console.WriteLine($"FileName {proxyFile}");
                await File.WriteAllTextAsync(proxyFile, script.Script);
				FindAndCopyCustomFiles(script.ReturnTypes, NamespacesJoined.Split(","), TargetBasePathForFiles);
                Console.WriteLine("Proxy file written");
				if (script.AddAbpResult)
					AddABPResult();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				Console.ReadKey();
			}

        }

		public static void FindAndCopyCustomFiles(IEnumerable<Type> types, IEnumerable<string> namespaces, string basePath){
			var outputAssembly = AssemblyLoader.LoadFromAssemblyPath(OutputAssembly);
			var customFiles = new List<CustomFile>();
			foreach (var type in types)
			{
				if (namespaces.Any(type.Namespace.Contains) && outputAssembly.GetType(type.Name) == null) {
    				var files = Directory.GetFileSystemEntries(new FileInfo(basePath).FullName, type.Name + ".cs", SearchOption.AllDirectories);
                    var item = files.FirstOrDefault();
    				if (item != null)
    					File.WriteAllText(Path.Combine(OutputDir, type.Name + ".cs"), File.ReadAllText(item));
				}
			}

		}

        public static void AddABPResult()
        {
            var proxyFileNameLocation = Environment.CurrentDirectory;
			if (!string.IsNullOrEmpty(OutputDir))
            {
                proxyFileNameLocation = Path.GetFullPath(OutputDir);
            }
            var newAbpResultPath = Path.Combine(proxyFileNameLocation, "ABPResult.cs");
            if (!File.Exists(newAbpResultPath))
                File.Copy(Path.Combine(Environment.CurrentDirectory, "ABPResult.cs"), newAbpResultPath);
            Console.WriteLine("ABPResult file written");
        }
    }
}
