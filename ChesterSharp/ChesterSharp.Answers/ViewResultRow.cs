using System;
using Newtonsoft.Json;

namespace ChesterSharp.Answers
{
    public class ViewResultRow<T> where T : CouchDocument, new() {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("rev")]
        public string Rev { get; set; }
        
        [JsonProperty("doc")]
        public T Doc { get; set; }
    }
}

