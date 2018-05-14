using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using NuGet.Configuration;

namespace ASO.GenerateBlazorProxies.Helpers
{
    public static class AssemblyLoader
    {
        public static Assembly LoadFromAssemblyPath(string assemblyFullPath)
        {
			var fileNameWithOutExtension = Path.GetFileNameWithoutExtension(assemblyFullPath);
            var fileName = Path.GetFileName(assemblyFullPath);
            var directory = Path.GetDirectoryName(assemblyFullPath);

            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyFullPath);
            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                // avoid loading *.resources dlls, because of: https://github.com/dotnet/coreclr/issues/8416
                if (name.Name.EndsWith("resources"))
                {
                    return null;
                }
                var depContext = DependencyContext.Default;
                var dependencies = depContext.RuntimeLibraries;
                foreach (var library in dependencies)
                {
                    if (IsCandidateLibrary(library, name))
                    {
                        var a = context.LoadFromAssemblyPath(library.Path);
                        return a;
                    }
                }
                
                //Get local nuget cache
                var settings = Settings.LoadDefaultSettings(null);
                var nugetCache = SettingsUtility.GetGlobalPackagesFolder(settings);

                IEnumerable<string> foundDlls = Directory.GetFileSystemEntries(new FileInfo(directory).FullName, name.Name + ".dll", SearchOption.AllDirectories);
                foundDlls = foundDlls.Concat(Directory.GetFileSystemEntries(nugetCache, name.Name + ".dll", SearchOption.AllDirectories));
                if (foundDlls.Any())
                {
                    return context.LoadFromAssemblyPath(foundDlls.First());
                }
				return context.LoadFromAssemblyName(name);
            };
            return assembly;
        }

        private static bool IsCandidateLibrary(Library library, AssemblyName assemblyName)
        {
            return (library.Name == (assemblyName.Name) || library.Dependencies.Any(d => d.Name == assemblyName.Name));
        }

    }
}
