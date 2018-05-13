﻿using ASO.GenerateBlazorProxies.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASO.GenerateBlazorProxies.MVC
{
    public class MVCBlazorProxyScriptGenerator
    {
        /// <summary>
        /// "Blazor".
        /// </summary>
        public const string Name = "Blazor";

        private bool _isABPResultAdded { get; set; } = false;

        public (string script, bool isABPResultAdded) CreateScript(IApiDescriptionGroupCollectionProvider apiDescriptionGroupCollectionProvider)
        {
            var script = new StringBuilder();

            script.AppendLine("/* This file is automatically generated by ASO to use MVC Controllers from Blazor. */");
            script.AppendLine();
            script.AppendLine();
            script.AppendLine("using Microsoft.AspNetCore.Blazor;");
            script.AppendLine("using System.Net.Http;");
            script.AppendLine("using System.Threading.Tasks;");
            GetNameSpaces(script, apiDescriptionGroupCollectionProvider.ApiDescriptionGroups.Items);
            script.AppendLine();
            script.AppendLine($"namespace ASPApp.Proxies");
            script.AppendLine("{");
            script.AppendLine($"    public class AppProxy");
            script.AppendLine("     {");
            script.AppendLine("         private readonly HttpClient _httpClient;");
            script.AppendLine($"        public AppProxy(HttpClient httpClient)");
            script.AppendLine("         {");
            script.AppendLine("             _httpClient = httpClient;");
            script.AppendLine("         }");

            foreach (var controller in apiDescriptionGroupCollectionProvider.ApiDescriptionGroups.Items)
            {
                AddControllerScript(script, controller);
            }
            
            script.AppendLine("     }");
            script.AppendLine("}");
            return (script.ToString(),_isABPResultAdded);
        }

        private void GetNameSpaces(StringBuilder script, IEnumerable<ApiDescriptionGroup> model)
        {
            var namespaces = new List<string>();
            foreach (var item in model.SelectMany(f => f.Items).SelectMany(f=>f.ActionDescriptor.Parameters).Select(f=>f.ParameterType).Distinct())
            {
                namespaces.Add(item.Namespace);
                namespaces.AddRange(item.GetGenericNamespaces());

            }
            foreach (var item in model.SelectMany(f => f.Items).SelectMany(f => f.SupportedResponseTypes).Select(f => f.Type).Distinct())
            {
                namespaces.Add(item.Namespace);
                namespaces.AddRange(item.GetGenericNamespaces());
            }

            foreach (var item in namespaces.Distinct())
            {
                script.AppendLine($"using {item};");
            }
        }

        private void AddControllerScript(StringBuilder script, ApiDescriptionGroup controller)
        {
            
            foreach (var action in controller.Items)
            {
                script.AppendLine();
                AddActionScript(script, controller, action);
            }
        }

        private  void AddActionScript(StringBuilder script, ApiDescriptionGroup controller, ApiDescription action)
        {
            script.AppendLine($"    //action {action.ActionDescriptor.DisplayName}");

            AddCallParameters(script, controller, action);
            script.AppendLine("        }");
        }

        private void AddCallParameters(StringBuilder script, ApiDescriptionGroup controller, ApiDescription action)
        {
            var methodParamNames = action.ParameterDescriptions.Select(p => $"{p.Type.Name} {p.Name}").Distinct().ToList();
            var parameterList = string.Join(", ",methodParamNames);
            var url = "/" + BlazorProxyHelper.GenerateUrlWithParameters(action);
            var httpMethod = action.HttpMethod?.ToUpperInvariant() ?? "POST";
            var postPara = "";

            var addAbpResult = BlazorProxyHelper.AddAbpResult(action);

            if (action.SupportedResponseTypes.Any(f => f.Type == typeof(void)) || !action.SupportedResponseTypes.Any())
            {
                script.AppendLine($"        public async Task {controller.GroupName}{(action.ActionDescriptor as ControllerActionDescriptor).ActionName}({parameterList})");
                script.AppendLine("        {");
            }
            else
            {
                script.AppendLine($"        public async Task<{BlazorProxyHelper.GetCSharpRepresentation(action.SupportedResponseTypes.First().Type, true)}> {controller.GroupName}{(action.ActionDescriptor as ControllerActionDescriptor).ActionName}({parameterList})");
                script.AppendLine("        {");
            }
            
            if (httpMethod == "POST" || httpMethod == "PUT")
            {
                var bodyParameters = action.ParameterDescriptions.Where(f => f.Source == null || f.Source.Id == "Body");
                if (bodyParameters.Any() && bodyParameters.Count() <= 1)
                    postPara = $", {bodyParameters.FirstOrDefault()?.Name}";
                else if (!bodyParameters.Any())
                {
                    httpMethod = "GET";
                }
                else if (bodyParameters.Count() > 1)
                    script.AppendLine("            throw new Exception(\"Can't have more than 1 parameter.\");");
            }

            var returnType = $"{BlazorProxyHelper.GetCSharpRepresentation(action.SupportedResponseTypes.First().Type, true)}";
            var returnHandle = new StringBuilder();
            if (!addAbpResult)
                returnHandle.AppendLine("             return result;");
            else
            {
                returnType = $"ABPResult<{returnType}>";
                returnHandle.AppendLine("              if (result.success)");
                returnHandle.AppendLine("              {");
                returnHandle.AppendLine("                   return result.result;");
                returnHandle.AppendLine("              }");
                returnHandle.AppendLine("              else");
                returnHandle.AppendLine("              {");
                returnHandle.AppendLine("                   if (result.unAuthorizedRequest)");
                returnHandle.AppendLine("                   {");
                returnHandle.AppendLine("                       throw new Exception(\"unAuthorizedRequest\");");
                returnHandle.AppendLine("                   }");
                returnHandle.AppendLine("               throw new Exception(result.error.message);");
                returnHandle.AppendLine("               }");
                if (!_isABPResultAdded)
                {
                    script.Insert(88, "using ASO.Shared;");
                    _isABPResultAdded = true;
                }
            }

            if (httpMethod == "DELETE")
            {
                script.AppendLine($"            var resultResponse = await _httpClient.{BlazorProxyHelper.FirstCharToUpper(httpMethod.ToLower())}Async(\"{url}\");");
                if (!action.SupportedResponseTypes.Any(f => f.Type == typeof(void)))
                {
                    script.AppendLine("             var resultString = await resultResponse.Content.ReadAsStringAsync();");
                    script.AppendLine($"            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<{returnType}>(resultString);");
                    script.AppendLine(returnHandle.ToString());
                }
                else
                {
                    script.AppendLine("            return;");
                }
            }
            else
            {
                if (!action.SupportedResponseTypes.Any(f => f.Type == typeof(void)))
                {
                    script.AppendLine($"            var result = await _httpClient.{BlazorProxyHelper.FirstCharToUpper(httpMethod.ToLower())}JsonAsync<{returnType}>(\"{url}\"{postPara});");
                    script.AppendLine(returnHandle.ToString());
                }
                else
                {
                    script.AppendLine($"           await _httpClient.{BlazorProxyHelper.FirstCharToUpper(httpMethod.ToLower())}JsonAsync(\"{url}\"{postPara});");
                    script.AppendLine("            return;");
                }
            }

        }
    }
}