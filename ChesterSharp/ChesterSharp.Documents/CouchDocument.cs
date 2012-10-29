using System;
using Newtonsoft.Json;

namespace ChesterSharp
{
    public class CouchDocument {
        [JsonProperty("_id")]
        public virtual string Id { get; set; }
        
        [JsonProperty("_rev")]
        public string Rev { get; set; }
    }
}
