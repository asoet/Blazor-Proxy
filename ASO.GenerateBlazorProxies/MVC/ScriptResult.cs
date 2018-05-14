using System;
using System.Collections.Generic;

namespace ASO.GenerateBlazorProxies.MVC
{
    public class ScriptResult
    {
        public ScriptResult()
        {
			ReturnTypes = new List<Type>();
        }

		public string Script { get; set; }
		public IEnumerable<Type> ReturnTypes { get; set; }
		public bool AddAbpResult { get; set; }
    }
}
