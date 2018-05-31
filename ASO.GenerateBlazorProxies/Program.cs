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
using System.Reflection;
using McMaster.Extensions.CommandLineUtils;
using System.Text.RegularExpressions;

namespace ASO.GenerateBlazorProxies
{
    class Program
    {
        [Option(Description = "target assembly path. In this assembly is the startup file.", ShortName = "ta", LongName = "targetassembly")]
        private static string TargetAssembly { get; set; }
        [Option(Description = "target base path. this path contains all the required return types to copy with the proxies.", ShortName = "tbp", LongName = "targetbasepath")]
        private static string TargetBasePathForFiles { get; set; }
        [Option(Description = "this is the dir to copy the proxies and return types to.", ShortName = "od", LongName = "outputdir")]
        private static string OutputDir { get; set; }
        [Option(Description = "this is the assembly to check if return types exists.", ShortName = "oa", LongName = "outputassembly")]
        private static string OutputAssembly { get; set; }
        [Option(Description = "the namespaces of the return types. seperate with ','", ShortName = "n", LongName = "namespaces")]
        private static string NamespacesJoined { get; set; }

        public static int Main(string[] args)
        => CommandLineApplication.Execute<Program>(args);


        private async Task OnExecute()
        {
            if (string.IsNullOrEmpty(TargetAssembly))
            {
                Console.WriteLine("Not targetAssembly found");
                Console.WriteLine("Try `--help' for more information.");
                return;
            }
            if (string.IsNullOrEmpty(TargetBasePathForFiles))
            {
                TargetBasePathForFiles = Directory.GetDirectoryRoot(TargetAssembly);
                Console.WriteLine("Not targetAssembly found");
                Console.WriteLine("Using targetAssembly path");
            }
            if (string.IsNullOrEmpty(OutputDir))
            {
                OutputDir = Directory.GetDirectoryRoot(TargetAssembly);
                Console.WriteLine("Not OutputDir found");
                Console.WriteLine("Try `--help' for more information.");
                return;
            }
            if (string.IsNullOrEmpty(OutputAssembly))
            {
                Console.WriteLine("Not OutputAssembly found");
                Console.WriteLine("skip check for exisiting types");
            }
            await GenerateMVC();
        }

        public static async Task GenerateMVC()
        {
            Console.WriteLine("Loading target Assembly");
            var assembly = AssemblyLoader.LoadFromAssemblyPath(TargetAssembly);
            Console.WriteLine("Loading target Assembly done");
            var startUpType = assembly.GetTypes().FirstOrDefault(f => f.Name == "Startup");
            if (startUpType == null)
            {
                Console.WriteLine("Loading target Assembly - No startup class found");
                return;
            }
            Console.WriteLine("Loading target Assembly - startup class found");
            try
            {
                Console.WriteLine("Loading target Assembly - Starting testServer");
                var testServer = new TestServer(new WebHostBuilder().ConfigureServices(services => {
                    services.TryAddSingleton<IApiDescriptionGroupCollectionProvider, ApiDescriptionGroupCollectionProvider>();
                    services.TryAddEnumerable(
                        ServiceDescriptor.Transient<IApiDescriptionProvider, DefaultApiDescriptionProvider>());
                    
                })
                 .UseStartup(startUpType));
                
                Console.WriteLine("Loading target Assembly - Done starting testServer");
                var apiModel = testServer.Host.Services.GetService<IApiDescriptionGroupCollectionProvider>();
                if (apiModel == null)
                {
                    Console.WriteLine("Loading target Assembly - No ApiExplorer found");
                    return;
                }
                Console.WriteLine("Generating proxies");
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
                Console.WriteLine("Exception:");
                Console.WriteLine(ex);
                Console.ReadKey();
            }

        }

        public static void FindAndCopyCustomFiles(IEnumerable<Type> types, IEnumerable<string> namespaces, string basePath)
        {
            Assembly outputAssembly = null;
            if (!string.IsNullOrEmpty(OutputAssembly))
                outputAssembly = AssemblyLoader.LoadFromAssemblyPath(OutputAssembly);
            var customFiles = new List<CustomFile>();
            foreach (var type in types)
            {
                if ((namespaces.Any(type.Namespace.Contains) || !namespaces.Any()) && (outputAssembly.GetType(type.Name) == null || outputAssembly == null))
                {
                    var files = Directory.GetFileSystemEntries(new FileInfo(basePath).FullName, type.Name + ".cs", SearchOption.AllDirectories);
                    var item = files.FirstOrDefault();
                    string regex = "(\\[.*\\])";
                    string output = Regex.Replace(item, regex, "");
                    if (item != null)
                        File.WriteAllText(Path.Combine(OutputDir, type.Name + ".cs"), File.ReadAllText(output));
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
