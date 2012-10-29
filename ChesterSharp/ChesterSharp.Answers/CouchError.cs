using System;
using Newtonsoft.Json;

namespace ChesterSharp
{
    public class CouchError {
        [JsonProperty("reason")]
        public string Reason { get; set; }
    }
}

