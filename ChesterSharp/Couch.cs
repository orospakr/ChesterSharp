using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Net.Http.Headers;
using System.Net;

using ChesterSharp.Exceptions;
using ChesterSharp.Answers;

namespace ChesterSharp
{
    /// <summary>
    /// A connection to a CouchDB server.
    /// 
    /// http://guide.couchdb.org/draft/api.html
    /// http://wiki.apache.org/couchdb/HTTP_Document_API
    /// http://en.wikipedia.org/wiki/CouchDB
    /// http://guide.couchdb.org/draft/design.html
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



        public CouchException HandleError(CouchError error, HttpStatusCode statusCode) {
            if (statusCode == HttpStatusCode.NotFound) {
                return new NotFoundException(error.Reason, statusCode);
            } else {
                return new CouchException(error.Reason, statusCode);
            }
        }

        public async Task<HttpResponseMessage> GetRawAsync(Uri uri) {
            Console.WriteLine("GET: {0}", uri.ToString());
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
            Console.WriteLine("DELETE: {0}", uri.ToString());
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
            Console.WriteLine("POST: {0}", uri.ToString());
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
            Console.WriteLine("PUT: {0}", uri.ToString());
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

        public string SerializeObject(CouchDocument doc) {
            return JsonConvert.SerializeObject(doc, Formatting.Indented, new JsonSerializerSettings {DefaultValueHandling = DefaultValueHandling.Ignore});
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
            } catch (NotFoundException) {
            }
        }

        public async Task<bool> DoesDatabaseExist(string database) {
            var uri = BuildDatabaseUri(database);
            var http = new HttpClient();
            var r = await http.GetAsync(uri);
            return r.StatusCode != HttpStatusCode.NotFound;
        }

        public async Task<DatabaseInfo> GetDatabaseInfo(string database) {
            // TODO factor apart get/updateDocument even more into single-document fetching code, usable from GetDocument and PutDocumentUpdate.
            var uri = BuildDatabaseUri(database);
            var r = await GetRawAsync(uri);
            var fetchedJson = await r.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<DatabaseInfo>(fetchedJson);
        }

        public async Task<CouchDatabase> OpenDatabase(String databaseName) {
            // will throw a NotFound error if database does not exist
            var dbInfo = await GetDatabaseInfo(databaseName);
            return new CouchDatabase(this, databaseName);
        }

        public Couch(string hostname, int port) {
            this.Hostname = hostname;
            this.Port = port;
        }
    }
}

