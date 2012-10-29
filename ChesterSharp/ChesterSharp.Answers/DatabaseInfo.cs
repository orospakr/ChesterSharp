using System;
using Newtonsoft.Json;

namespace ChesterSharp
{
    public class DatabaseInfo {
        [JsonProperty("db_name")]
        public string Name { get; set; }
        
        [JsonProperty("doc_count")]
        public Int64 DocumentCount { get; set; }
        
        [JsonProperty("doc_del_count")]
        public Int64 DeletedDocumentCount { get; set; }
        
        [JsonProperty("update_seq")]
        public Int64 UpdateSequenceNumber { get; set; }
        
        [JsonProperty("purge_seq")]
        public Int64 PurgeSequenceNumber { get; set; }
        
        [JsonProperty("compact_running")]
        public bool CompactRunning { get; set; }
        
        [JsonProperty("disk_size")]
        public Int64 DiskSize { get; set; }
        
        [JsonProperty("data_size")]
        public Int64 DataSize { get; set; }
        
        // TODO make a usec DateTimeConverter
        // [JsonProperty("instance_start_time")]
        
        [JsonProperty("disk_format_version")]
        public int DiskFormatVersion { get; set; }
        
        [JsonProperty("committed_update_seq")]
        public Int64 CommittedUpdateSeq { get; set; }
    }
}

