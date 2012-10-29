using System;
using Newtonsoft.Json;

namespace ChesterSharp.Answers
{
    public class ActionResult {
        [JsonProperty("ok")]
        public bool OK { get; set; }
    }
}

