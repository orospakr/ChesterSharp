using System;
using Newtonsoft.Json;

namespace ChesterSharp.Answers
{
    public class DocumentCreationResult : ActionResult {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("rev")]
        public string Rev { get; set; }
    }
}

