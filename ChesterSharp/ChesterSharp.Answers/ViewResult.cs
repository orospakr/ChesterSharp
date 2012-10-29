using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ChesterSharp.Answers
{
    public class ViewResult<T> where T : CouchDocument, new() {
        [JsonProperty("total_rows")]
        public int TotalRows { get; set; }
        
        [JsonProperty("offset")]
        public int Offset { get; set; }
        
        [JsonProperty("rows")]
        public List<ViewResultRow<T>> Rows { get; set; }
    }
}

