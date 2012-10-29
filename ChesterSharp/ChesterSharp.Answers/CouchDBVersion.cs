using System;
using Newtonsoft.Json;

namespace ChesterSharp.Answers
{
    /// <summary>
    /// CouchDB version information as returned by the root path on a CouchDB's HTTP server.
    /// 
    /// Looks like:
    /// {"couchdb":"Welcome","version":"1.2.0"}
    /// 
    /// </summary>
    public class CouchDBVersion
    {
        [JsonProperty("couchdb")]
        public string CouchDB { get; set; }
        
        [JsonProperty("version")]
        public string Version { get; set; }
    }
}

