# Blazor-Proxy-generator
With this project you can create a proxy file for your blazor application using the ASP.NET with or without the [ABP framework ](https://aspnetboilerplate.com/). 
# Use
Run with arguments:
1. -ta target assembly path. In this assembly is the startup file. path to dll.
2. -tbp target base path. This is the path of all the return and parameter types to find and copy to the output dir.
3. -n all the namespaces (or part of) of the return and parameter types to find and copy
4. -od output path. All the files will be copied to this path. For example the shared library.
5. -oa this is the assembly (dll) of the output project, to check if the return/parameter type already exists. 

Add the generateBlazorProxies project as post build in your server project. 

When .NET core 2.1 is released I will release a global tool.

# todo
- [ ] convert to a dotnet tool
- [x] Add blazor test project
- [ ] Add unit tests
