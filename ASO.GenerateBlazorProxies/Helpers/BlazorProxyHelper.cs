using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ASO.GenerateBlazorProxies.Helpers
{
    internal static class BlazorProxyHelper
    {

        internal static IEnumerable<MethodInfo> GetActions(this Type t)
        {
            return t.GetMethods()
                    .Where(method => method.IsPublic && !method.IsDefined(typeof(NonActionAttribute)));
        }
        internal static IEnumerable<string> GetGenericNamespaces(this Type t)
        {
            var namespaces = new List<string>();
            foreach (var item in t.GetGenericArguments())
            {
                namespaces.Add(item.Namespace);
            }
            return namespaces.Distinct();
        }

        internal static string GetCSharpRepresentation(Type t, bool trimArgCount)
        {
            if (t.IsGenericType)
            {
                var genericArgs = t.GetGenericArguments().ToList();

                return GetCSharpRepresentation(t, trimArgCount, genericArgs);
            }

            return t.Name;

        }

        internal static string GetCSharpRepresentation(Type t, bool trimArgCount, List<Type> availableArguments)
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
                    value = GetCSharpRepresentation(t.DeclaringType, trimArgCount, availableArguments) + "+" + value;
                }

                // Build the type arguments (if any)
                string argString = "";
                var thisTypeArgs = t.GetGenericArguments();
                for (int i = 0; i < thisTypeArgs.Length && availableArguments.Count > 0; i++)
                {
                    if (i != 0) argString += ", ";

                    argString += GetCSharpRepresentation(availableArguments[0], trimArgCount);
                    availableArguments.RemoveAt(0);
                }

                // If there are type arguments, add them with < >
                if (argString.Length > 0)
                {
                    value += "<" + argString + ">";
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

        internal static string GenerateUrlWithParameters(ApiDescription action)
        {
            
            var parameters = action.ParameterDescriptions;
            var url = ReplacePathVariables(action.RelativePath, parameters);
            url = AddQueryStringParameters(url, parameters);
            return url;
        }

        internal static string ReplacePathVariables(string url, IList<ApiParameterDescription> actionParameters)
        {
            var pathParameters = actionParameters
                .Where(p => p.Source != null && p.Source.Id == "Path")
                .ToArray();

            if (!pathParameters.Any())
            {
                return url;
            }

            foreach (var pathParameter in pathParameters)
            {
                url = url.Replace($"{{{pathParameter.Name}}}", $"\" + {pathParameter.Name} + \"");
            }

            return url;
        }

        internal static string AddQueryStringParameters(string url, IList<ApiParameterDescription> actionParameters)
        {
            var queryStringParameters = actionParameters
                .Where(p =>p.Source.Id == null || p.Source.Id.IsIn("ModelBinding", "Query"))
                .ToArray();

            if (!queryStringParameters.Any())
            {
                return url;
            }

            var qsBuilderParams =string.Join("&", queryStringParameters
                .Select(p => $"{p.Name}=\"+{p.Name}+\""));

            return url + $"?{qsBuilderParams}";
        }

        internal static bool AddAbpResult(ApiDescription actionDescription)
        {
            var shouldAdd = true;
            var action = (ControllerActionDescriptor)actionDescription.ActionDescriptor;
            if (action == null)
            {
                return false;
            }

            //Try to get for dynamic APIs (dynamic web api actions always define __AbpDynamicApiDontWrapResultAttribute)
            if (action.Properties.GetOrDefault("__AbpDynamicApiDontWrapResultAttribute") != null)
            {
                shouldAdd = false;
            }

            //Get for the action
            if (action.MethodInfo.GetCustomAttributes(true).FirstOrDefault(f => f.GetType().Name == "WrapResultAttribute") != null)
            {
                shouldAdd = false;
            }

            //Get for the controller
            if (action.ControllerTypeInfo.GetCustomAttributes(true).FirstOrDefault(f => f.GetType().Name == "WrapResultAttribute") != null)
            {
                shouldAdd = false;
            }

            var type = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(f => f.Name == "AbpServiceBase" && f.Namespace == "Abp");
            //Check if controller is ABP
            if (type != null && !action.ControllerTypeInfo.IsSubclassOf(type))
            {
                shouldAdd = false; 
            }
            if (type == null)
                shouldAdd = false;

            //Not found
            return shouldAdd;
        }

        /// <summary>
        /// Check if an item is in a list.
        /// </summary>
        /// <param name="item">Item to check</param>
        /// <param name="list">List of items</param>
        /// <typeparam name="T">Type of the items</typeparam>
        public static bool IsIn<T>(this T item, params T[] list)
        {
            return list.Contains(item);
        }
        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.TryGetValue(key, out TValue obj) ? obj : default;
        }
    }
}
