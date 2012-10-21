using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;

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

    /// <summary>
    /// Couch.
    /// 
    /// http://guide.couchdb.org/draft/api.html
    /// http://wiki.apache.org/couchdb/HTTP_Document_API
    /// 
    /// </summary>
    public class Couch
    {
        private string Hostname;
        private int Port;

        /// <summary>
        /// Joins given directory of file name path elements into a / separated path suitable for HTTP.
        /// </summary>
        /// <returns>
        /// The join.
        /// </returns>
        /// <param name='parts'>
        /// Parts.
        /// </param>
//        public static String uriBuild(params string[] parts) {
//            var finalList = new Stack<String>();
//            foreach(var part in parts) {
//                if(part.Contains("/")) {
//                    throw new ArgumentException("Path parts should not contain directory separators.");
//                }
//                if(part.Length == 0) {
//                    continue;
//                }
//                if("..".Equals(part)) {
//                    finalList.Pop();
//                }
//                // var x = new System.Net.Http.FormUrlEncodedContent(
//                finalList.Push(Uri.EscapeDataString(part));
//            }
//            return "/" + String.Join("/", finalList.ToArray());
//        }

        public static string uriJoin(String basePath, String relativePath) {
            if(basePath == null || basePath.Length == 0) {
                return String.Format("/{1}", relativePath); 
            } else if (basePath.EndsWith("/")) {
                return String.Format("{0}{1}", basePath, relativePath);
            } else {
                return String.Format("{0}/{1}", basePath, relativePath);
            }
        }

        public static Uri uriJoin(Uri baseUri, String relativePath) {
            // the + "/" may be a workaround for mono, not sure
            return new Uri(baseUri, uriJoin(baseUri.AbsolutePath, relativePath));
        }

        public Uri buildServerUri() {
            var url = new UriBuilder();
            url.Host = this.Hostname;
            url.Port = this.Port;
            url.Scheme = "http";
            return url.Uri;
        }

        public Uri buildDatabaseUri(String database) {
            return uriJoin(buildServerUri(), database);
        }

        public Uri buildDocumentUri(String database, String id) {
            var dbUri = buildDatabaseUri(database);
            return uriJoin(dbUri, id);
        }

        public async Task<String> getServerVersion() {

            var http = new System.Net.Http.HttpClient();
            var uri = buildServerUri();
            Console.WriteLine("Fetching: {0}", uri.ToString());
            // http.BaseAddress = uri;

            var response = await http.GetAsync(uri);
            response.EnsureSuccessStatusCode();

            if(response.StatusCode != System.Net.HttpStatusCode.OK) {
                // TODO throw our own exception type
                throw new Exception("Document does not exist.");
            }

            var fetchedJson = await response.Content.ReadAsStringAsync();

            var serverInfo = JsonConvert.DeserializeObject<CouchDBVersion>(fetchedJson);

            return serverInfo.Version;
        }

        public async Task<String> getRawDocument(String database, String id) {
            var http = new System.Net.Http.HttpClient();

            var uri = buildDocumentUri(database, id);

            Console.WriteLine("Fetching document from: {0}", uri);

            var response = await http.GetAsync(uri);
            response.EnsureSuccessStatusCode();

            if(response.StatusCode != System.Net.HttpStatusCode.OK) {
                // TODO throw our own exception type
                throw new Exception("Document does not exist.");
            }

            return await response.Content.ReadAsStringAsync();
        }

        /// </param>
        public async Task<T> getDocument<T>(String database, String id) where T : CouchDocument {
            var fetchedJson = await getRawDocument(database, id);
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
        public async Task<String> postRawDocumentUpdate(String database, String content, String id) {
            var http = new System.Net.Http.HttpClient();
            
            var uri = buildDocumentUri(database, id);
            Console.WriteLine("Posting document to: {0}", uri);

            var response = await http.PostAsync(uri, new StringContent(content));

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }


        public static Task<string> fetchUrlTest() {
            var http = new System.Net.Http.HttpClient();

            var tcs = new TaskCompletionSource<String>();
            
            http.GetAsync("http://www.debian.org").ContinueWith((request) => {
                var response = request.Result;
                response.EnsureSuccessStatusCode();
                response.Content.ReadAsStringAsync().ContinueWith((bodyResult) => {
                    if (bodyResult.IsFaulted) {
                        throw bodyResult.Exception;
                    }
                    Console.Out.WriteLine(bodyResult.Result);
                    tcs.SetResult(bodyResult.Result);
                });
            });
            return tcs.Task;
        }

        public static async Task<string> fetchUrlTestNew() {
            var http = new System.Net.Http.HttpClient();
            var response = await http.GetStringAsync("http://www.debian.org");
            return response;
        }

        public Couch(string hostname, int port) {
            this.Hostname = hostname;
            this.Port = port;
        }
    }
}

