using Abp.Collections.Extensions;
using Abp.Extensions;
using Abp.Web.Api.Modeling;
using Abp.Web.Api.ProxyScripting.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Abp.GenerateBlazorProxies.Helpers
{
    internal static class BlazorProxyHelper
    {
        internal static IEnumerable<string> GetGenericNamespaces(this Type t)
        {
            var namespaces = new List<string>();
            foreach (var item in t.GetGenericArguments())
            {
                namespaces.Add(item.Namespace);
            }
            return namespaces.Distinct();
        }

        internal static string GetCSharpRepresentation(Type t, bool trimArgCount, bool removeTask)
        {
            if (t.IsGenericType)
            {
                var genericArgs = t.GetGenericArguments().ToList();

                return GetCSharpRepresentation(t, trimArgCount, genericArgs, removeTask);
            }

            return t.Name;

        }

        internal static string GetCSharpRepresentation(Type t, bool trimArgCount, List<Type> availableArguments, bool removeTask)
        {
            if (t.IsGenericType)
            {
                string value = t.Name;
                if (trimArgCount && value.IndexOf("`") > -1)
                {
                    value = value.Replace("`1", "");
                }

                if (t.DeclaringType != null)
                {
                    // This is a nested type, build the nesting type first
                    value = GetCSharpRepresentation(t.DeclaringType, trimArgCount, availableArguments, false) + "+" + value;
                }

                // Build the type arguments (if any)
                string argString = "";
                var thisTypeArgs = t.GetGenericArguments();
                for (int i = 0; i < thisTypeArgs.Length && availableArguments.Count > 0; i++)
                {
                    if (i != 0) argString += ", ";

                    argString += GetCSharpRepresentation(availableArguments[0], trimArgCount, false);
                    availableArguments.RemoveAt(0);
                }

                // If there are type arguments, add them with < >
                if (argString.Length > 0 && !removeTask)
                {
                    value += "<" + argString + ">";
                }
                else if (removeTask)
                {
                    return argString;
                }

                return value;
            }

            return t.Name;
        }
        internal static string FirstCharToUpper(string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input.First().ToString().ToUpper() + input.Substring(1);
            }
        }

        internal static string GenerateUrlWithParameters(ActionApiDescriptionModel action)
        {
            var url = ReplacePathVariables(action.Url, action.Parameters);
            url = AddQueryStringParameters(url, action.Parameters);
            return url;
        }
        internal static string ReplacePathVariables(string url, IList<ParameterApiDescriptionModel> actionParameters)
        {
            var pathParameters = actionParameters
                .Where(p => p.BindingSourceId == ParameterBindingSources.Path)
                .ToArray();

            if (!pathParameters.Any())
            {
                return url;
            }

            foreach (var pathParameter in pathParameters)
            {
                url = url.Replace($"{{{pathParameter.Name}}}", $"' + {ProxyScriptingJsFuncHelper.GetParamNameInJsFunc(pathParameter)} + '");
            }

            return url;
        }
        internal static string AddQueryStringParameters(string url, IList<ParameterApiDescriptionModel> actionParameters)
        {
            var queryStringParameters = actionParameters
                .Where(p => p.BindingSourceId.IsIn(ParameterBindingSources.ModelBinding, ParameterBindingSources.Query))
                .ToArray();

            if (!queryStringParameters.Any())
            {
                return url;
            }

            var qsBuilderParams = queryStringParameters
                .Select(p => $"{p.Name.ToCamelCase()}=\"+{p.Name}+\"")
                .JoinAsString("&");

            return url + $"?{qsBuilderParams}";
        }
    }
}
