# Blazor-Proxy-generator
With this project you can create a proxy file for your blazor application using the ASP.NET works with the [ABP framework ](https://aspnetboilerplate.com/). 
# Use


1. run ```dotnet tool install --global DotnetBlazorProxy --version 0.1.6```.
2. Add the generateBlazorProxies project as post build in your server project (change paths):
```
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Exec Condition=" '$(Configuration)'=='Debug' " Command="blazorproxy -ta $(TargetPath) -tbp $(SolutionDir) -od $(SolutionDir)\Shared\ -oa $(SolutionDir)\Shared\bin\Debug\netstandard2.0\Shared.dll -n ASO" />
    </Target>
 ```
 
arguments:
* -ta target assembly path. In this assembly is the startup file. path to dll.
* -tbp target base path. This is the path of all the return and parameter types to find and copy to the output dir.
* -n all the namespaces (or part of) of the return and parameter types to find and copy
* -od output path. All the files will be copied to this path. For example the shared library.
* -oa this is the assembly (dll) of the output project, to check if the return/parameter type already exists. 

This is a dotnet global tool.

