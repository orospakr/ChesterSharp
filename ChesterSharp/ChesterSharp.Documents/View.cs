using System;
using Newtonsoft.Json;

namespace ChesterSharp.Documents
{
    public abstract class View {
        // TODO: var processed = pair.Value.Replace("$MODEL_NAME", String.Format("\"{0}\"", ModelName()));
        
        [JsonProperty("map")]
        public abstract String Map { get; }
        
        [JsonProperty("reduce")]
        public virtual String Reduce { get { return null; } }
        
        public static string GetViewName(Type t) {
            // TODO check for an attribute override
            return t.Name.ToLowerInvariant();
        }
    }
}

