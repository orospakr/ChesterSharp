using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Net.Http.Headers;
using System.Net;

namespace SharpCouch
{
    /// <summary>
    /// CouchDB version information.
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

    public class CouchDocument {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("rev")]
        public string Rev { get; set; }
    }

    public class CouchError  {
        [JsonProperty("reason")]
        public string Reason { get; set; }
    }

    public class ActionResult {
        [JsonProperty("ok")]
        public bool OK { get; set; }
    }

    public class DocumentCreationResult : ActionResult {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("rev")]
        public string Rev { get; set; }
    }

    public class CouchException : Exception {
        public HttpStatusCode StatusCode { get; set; }

        public CouchException(string message, HttpStatusCode statusCode) : base(string.Format("[CouchException: StatusCode={0}, Message: {1}]", statusCode, message)) {
        }
    }

    public class NotFoundException : CouchException {
        public NotFoundException(string message, HttpStatusCode statusCode) : base(message, statusCode) {
        }
    }

    public class CouchDatabase {
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
        public bool CompactRUnning { get; set; }

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

    /// <summary>
    /// Couch.
    /// 
    /// http://guide.couchdb.org/draft/api.html
    /// http://wiki.apache.org/couchdb/HTTP_Document_API
    /// http://en.wikipedia.org/wiki/CouchDB
    /// 
    /// </summary>
    public class Couch
    {
        private string Hostname;
        private int Port;

        public static string UriJoin(String basePath, String relativePath) {
            if(basePath == null || basePath.Length == 0) {
                return String.Format("/{1}", relativePath); 
            } else if (basePath.EndsWith("/")) {
                return String.Format("{0}{1}", basePath, relativePath);
            } else {
                return String.Format("{0}/{1}", basePath, relativePath);
            }
        }

        public static Uri UriJoin(Uri baseUri, String relativePath) {
            // the + "/" may be a workaround for mono, not sure
            return new Uri(baseUri, UriJoin(baseUri.AbsolutePath, relativePath));
        }

        public Uri BuildServerUri() {
            var url = new UriBuilder();
            url.Host = this.Hostname;
            url.Port = this.Port;
            url.Scheme = "http";
            return url.Uri;
        }

        public Uri BuildDatabaseUri(String database) {
            return UriJoin(BuildServerUri(), database);
        }

        public Uri BuildDocumentUri(String database, String id) {
            var dbUri = BuildDatabaseUri(database);
            return UriJoin(dbUri, id);
        }

        public CouchException HandleError(CouchError error, HttpStatusCode statusCode) {
            if (statusCode == HttpStatusCode.NotFound) {
                return new NotFoundException(error.Reason, statusCode);
            } else {
                return new CouchException(error.Reason, statusCode);
            }
        }

        public async Task<HttpResponseMessage> GetRawAsync(Uri uri) {
            var http = new System.Net.Http.HttpClient();
            var response = await http.GetAsync(uri);
            if(response.IsSuccessStatusCode) {
                return response;
            } else {
                var fetchedErrorJson = await response.Content.ReadAsStringAsync();
                var error = JsonConvert.DeserializeObject<CouchError>(fetchedErrorJson);
                throw HandleError(error, response.StatusCode);
            }
        }

        public async Task<HttpResponseMessage> DeleteRawAsync(Uri uri) {
            var http = new System.Net.Http.HttpClient();
            var response = await http.DeleteAsync(uri);
            if(response.IsSuccessStatusCode) {
                return response;
            } else {
                var fetchedErrorJson = await response.Content.ReadAsStringAsync();
                var error = JsonConvert.DeserializeObject<CouchError>(fetchedErrorJson);
                throw HandleError(error, response.StatusCode);
            }
        }

        public async Task<HttpResponseMessage> PostRawAsync(Uri uri, HttpContent content) {
            var http = new System.Net.Http.HttpClient();
            var response = await http.PostAsync(uri, content);
            if(response.IsSuccessStatusCode) {
                return response;
            } else {
                var fetchedErrorJson = JsonConvert.DeserializeObject<CouchError>(await response.Content.ReadAsStringAsync());
                throw HandleError(fetchedErrorJson, response.StatusCode);
            }
        }

        public async Task<HttpResponseMessage> PutRawAsync(Uri uri, HttpContent content) {
            var http = new System.Net.Http.HttpClient();
            var response = await http.PutAsync(uri, content);
            if(response.IsSuccessStatusCode) {
                return response;
            } else {
                var fetchedErrorJson = JsonConvert.DeserializeObject<CouchError>(await response.Content.ReadAsStringAsync());
                throw HandleError(fetchedErrorJson, response.StatusCode);
            }
        }

        public async Task<String> GetServerVersion() {
            var uri = BuildServerUri();
            Console.WriteLine("Fetching: {0}", uri.ToString());
            // http.BaseAddress = uri;

            var response = await GetRawAsync(uri);

            var fetchedJson = await response.Content.ReadAsStringAsync();

            var serverInfo = JsonConvert.DeserializeObject<CouchDBVersion>(fetchedJson);

            return serverInfo.Version;
        }

        public async Task<String> GetRawDocument(String database, String id) {

            var uri = BuildDocumentUri(database, id);

            Console.WriteLine("Fetching document from: {0}", uri);

            var response = await GetRawAsync(uri);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        /// </param>
        public async Task<T> GetDocument<T>(String database, String id) where T : CouchDocument {
            var fetchedJson = await GetRawDocument(database, id);
            return JsonConvert.DeserializeObject<T>(fetchedJson);
        }

        /// <summary>
        /// Updates a document with arbitrary string data.
        /// </summary>
        /// <returns>
        /// Result data produced by CouchDB.  JSON with just id and rev, usually.
        /// </returns>
        /// <param name='content'>
        /// String data.  Remember, CouchDB expects JSON.
        /// </param>
        /// <param name='id'>
        /// ID of the document being updated.
        /// </param>
        public async Task<String> PutRawDocumentUpdate(String database, String content, String id) {
            
            var uri = BuildDocumentUri(database, id);
            Console.WriteLine("Posting document to: {0}", uri);

            var httpContent = new StringContent(content);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await PutRawAsync(uri, httpContent);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<T> PutDocumentUpdate<T>(String database, T content, String id) where T: CouchDocument {
            var json = JsonConvert.SerializeObject(content);
            var resultantJson = await PutRawDocumentUpdate(database, json, id);
            return JsonConvert.DeserializeObject<T>(resultantJson);
        }

        public async Task CreateDatabase(String database) {
            var uri = BuildDatabaseUri(database);
            await PutRawAsync(uri, null);
        }

        public async Task DeleteDatabase(String database) {
            var uri = BuildDatabaseUri(database);
            await DeleteRawAsync(uri);
        }

        /// <summary>
        /// Attempts to delete the provided database, and if already does not exist it succeeds.
        /// </summary>
        public async Task EnsureDatabaseDeleted(String database) {
            try {
                await DeleteDatabase(database);
            } catch (AggregateException ae) {
                ae.Handle((e) => {
                    if(e is NotFoundException) {
                        return true;
                    }
                    return false;
                });
            }
        }

        public async Task<bool> DoesDatabaseExist(string database) {
            var uri = BuildDatabaseUri(database);
            var http = new HttpClient();
            var r = await http.GetAsync(uri);
            return r.StatusCode != HttpStatusCode.NotFound;
        }

        public async Task<CouchDatabase> GetDatabaseInfo(string database) {
            // TODO factor apart get/updateDocument even more into single-document fetching code, usable from GetDocument and PutDocumentUpdate.
            var uri = BuildDatabaseUri(database);
            var r = await GetRawAsync(uri);
            var fetchedJson = r.Content.ToString();
            return JsonConvert.DeserializeObject<CouchDatabase>(fetchedJson);
        }

        public Couch(string hostname, int port) {
            this.Hostname = hostname;
            this.Port = port;
        }
    }
}

