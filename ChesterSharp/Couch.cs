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
        [JsonProperty("_id")]
        public virtual string Id { get; set; }

        [JsonProperty("_rev")]
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

    public abstract class View {
        // TODO: var processed = pair.Value.Replace("$MODEL_NAME", String.Format("\"{0}\"", ModelName()));

        [JsonProperty("map")]
        public abstract String Map { get; }

        [JsonProperty("reduce")]
        public virtual String Reduce { get { return null; } }

        public static string GetViewName(Type t) {
            // TODO check for an attribute override
            return t.Name.ToLowerInvariant();
        }
    }

    public class DesignDocument : CouchDocument {
        [AttributeUsage(AttributeTargets.Class)]
        protected class DesignDocumentName : System.Attribute {
            public string Name { get; set; }
            public DesignDocumentName(string name) {
                Name = name;
            }
        }

        public static string GetDesignDocumentName(Type t) {
            var attrs = t.GetCustomAttributes(typeof(DesignDocument.DesignDocumentName), true);
            if(attrs.Length == 0) {
                return t.Name;
            } else {
                return ((DesignDocumentName)attrs[0]).Name;
            }
        }

        public static string GetDesignDocumentName<T>() where T : DesignDocument {
            var t = typeof(T);
            return GetDesignDocumentName(t);
        }

        [JsonProperty("views")]
        public Dictionary<string, View> Views
        {
            get
            {
                var result = new Dictionary<string, View>();
                foreach(var viewType in this.GetType().GetNestedTypes()) {
                    if(typeof(View).IsAssignableFrom(viewType)) {
                        var constructors = viewType.GetConstructor(Type.EmptyTypes);
                        var viewObj = constructors.Invoke(new object[] { });
                        result.Add(View.GetViewName(viewType), (View)viewObj);
                    }
                }
                return result;
            }
        }

        public string GetName() {
            var type = this.GetType();
            return GetDesignDocumentName(type);
        }

        [JsonProperty("_id")]
        public override string Id
        {
            get
            {
                return String.Format("_design/{0}", this.GetName());
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public DesignDocument() {
        }
    }

    public class DocumentCreationResult : ActionResult {
        [JsonProperty("id")]
        public string Id { get; set; }
        
        [JsonProperty("rev")]
        public string Rev { get; set; }
    }

    public class ViewResultRow<T> where T : CouchDocument, new() {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("rev")]
        public string Rev { get; set; }

        [JsonProperty("doc")]
        public T Doc { get; set; }
    }

    public class ViewResult<T> where T : CouchDocument, new() {
        [JsonProperty("total_rows")]
        public int TotalRows { get; set; }

        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("rows")]
        public List<ViewResultRow<T>> Rows { get; set; }
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

    public class CouchDatabase {
        private string Database;
        private Couch CouchDB;

        public CouchDatabase(Couch couch, string databaseName) {
            this.CouchDB = couch;
            this.Database = databaseName;
        }

        public Uri BuildDocumentUri(String id) {
            var dbUri = CouchDB.BuildDatabaseUri(Database);
            return Couch.UriJoin(dbUri, id);
        }

        public Uri BuildDesignDocumentUri(String designDocumentName) {
            var dbUri = CouchDB.BuildDatabaseUri(Database);
            return Couch.UriJoin(Couch.UriJoin(dbUri, "_design"), designDocumentName);
        }
        
        public Uri BuildViewUri(String designDocumentName, String viewName) {
            return Couch.UriJoin(Couch.UriJoin(BuildDesignDocumentUri(designDocumentName), "_view"), viewName);
        }
        
        public async Task<String> GetRawDocument(String id) {
            var uri = BuildDocumentUri(id);
            
            Console.WriteLine("Fetching document from: {0}", uri);
            
            var response = await CouchDB.GetRawAsync(uri);
            
            return await response.Content.ReadAsStringAsync();
        }
        
        /// </param>
        public async Task<T> GetDocument<T>(String id) where T : CouchDocument {
            var fetchedJson = await GetRawDocument(id);
            return JsonConvert.DeserializeObject<T>(fetchedJson);
        }
        
        public async Task<T> GetDesignDocument<T>(String id) where T : DesignDocument {
            var fetchedJson = await GetRawDocument(id);
            return JsonConvert.DeserializeObject<T>(fetchedJson);
        }

        public async Task<T> PutDocument<T>(T content, String id) where T: CouchDocument {
            var json = CouchDB.SerializeObject(content);
            var resultantJson = await PutRawDocument(json, id);
            // TODO; this will screw up, becase we do *NOT* get the whole object back.  instead, we must make an UpdateResult object, deserialize to that, and set them back into the POCO
            return JsonConvert.DeserializeObject<T>(resultantJson);
        }
        
        /// <summary>
        /// Updates or creates a document, with specified id, with arbitrary string data.
        /// </summary>
        /// <returns>
        /// Result data produced by CouchDB.  JSON with just id and rev, usually.
        /// </returns>
        /// <param name='content'>
        /// String data.  Remember, CouchDB expects JSON.
        /// </param>
        /// <param name='id'>
        /// ID of the document being created/updated.
        /// </param>
        public async Task<String> PutRawDocument(String content, String id) {
            var uri = BuildDocumentUri(id);
            Console.WriteLine("Posting document to: {0}", uri);

            var httpContent = new StringContent(content);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            
            var response = await CouchDB.PutRawAsync(uri, httpContent);
            
            return await response.Content.ReadAsStringAsync();
        }
        
        public async Task<String> PostRawDocument(String content) {
            var uri = CouchDB.BuildDatabaseUri(Database);
            
            var httpContent = new StringContent(content);
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            
            var response = await CouchDB.PostRawAsync(uri, httpContent);
            
            return await response.Content.ReadAsStringAsync();
        }
        
        /// <summary>
        /// Updates an existing document in the database.
        /// </summary>
        /// <returns>
        /// The document.
        /// </returns>
        /// <param name='database'>
        /// Database name.
        /// </param>
        /// <param name='content'>
        /// The Document object to update.  Id and Rev must both be set.
        /// </param>
        /// <typeparam name='T'>
        /// The document type.
        /// </typeparam>
        public async Task<T> UpdateDocument<T>(T content) where T: CouchDocument {
            if(content.Id == null || content.Id.Length == 0) {
                throw new ArgumentOutOfRangeException(String.Format("Document to update (of type {0}) must have Id set (got back '{1}').", typeof(T).Name, content.Id));
            }
            if(content.Rev == null || content.Rev.Length == 0) {
                throw new ArgumentOutOfRangeException("Document to update must have current Rev set.");
            }
            return await PutDocument<T>(content, content.Id);
        }
        
        public async Task<T> CreateDocument<T>(T content) where T: CouchDocument {
            if(content.Rev != null) {
                throw new ArgumentException(String.Format("New documents must not have a parent rev. '{0}' specified.", content.Rev));
            }
            if(content.Id != null) {
                return await PutDocument<T>(content, content.Id);
            } else {
                return await PostDocument<T>(content);
            }
        }
        
        public async Task<T> PostDocument<T>(T content) where T: CouchDocument {
            var json = CouchDB.SerializeObject(content);
            var resultantJson = await PostRawDocument(json);
            var result = JsonConvert.DeserializeObject<DocumentCreationResult>(resultantJson);
            content.Id = result.Id;
            content.Rev = result.Rev;
            return content;
        }

        public async Task<DD> UpdateDesignDocument<DD>() where DD : DesignDocument, new() {
            // while the DesignDocument objects themselves are intended to have no runtime state,
            // an instanced version is used for serialization and submission.
            var pd = new DD();

            try {
                // just use the base CouchDocument instead of DesignDocument, because DesignDocument
                // is not meant to be mutable and therefore isn't deserializable
                Console.WriteLine("Checking for existing design document for {0}...", pd.Id);
                var existing = await this.GetDocument<CouchDocument>(pd.Id);
                pd.Rev = existing.Rev;
                Console.WriteLine("DD {0} already exists, replacing it.", pd.Id);
                return await this.UpdateDocument<DD>(pd);
            } catch (NotFoundException) {
                Console.WriteLine("DD {0} doesn't already exist, creating it.", pd.Id);
            }
            return await this.CreateDocument<DD>(pd);
        }

        public async Task<string> GetViewRaw(String designDocName, String viewName, bool includeDocs) {
            var uri = BuildViewUri(designDocName, viewName);
            if(includeDocs) {
            var builder = new UriBuilder(uri);
                builder.Query += "include_docs=true";
                uri = builder.Uri;
            }
            var r = await CouchDB.GetRawAsync(uri);
            return await r.Content.ReadAsStringAsync();
        }

        // http://wiki.apache.org/couchdb/HTTP_view_API
        public async Task<IEnumerable<T>> GetDocsFromView<T>(String designDocName, String viewName) where T : CouchDocument, new() {
            var fetchedJson = await GetViewRaw(designDocName, viewName, true);
            var viewResult = JsonConvert.DeserializeObject<ViewResult<T>>(fetchedJson);
            return from c in viewResult.Rows select c.Doc;
        }
        
        /// <summary>
        /// Gets the contents of the view, specified by means of the programmatic local
        /// representations of the Design Document and the View.
        /// </summary>
        /// <returns>
        /// All of the 
        /// </returns>
        /// <param name='database'>
        /// Database name.
        /// </param>
        /// <typeparam name='D'>
        /// DesignDocument class that contains the view.
        /// </typeparam>
        /// <typeparam name='V'>
        /// View class, as usually nested within the DesignDocument class.
        /// </typeparam>
        /// <typeparam name='T'>
        /// CouchDocument type for the actual documents received back from the view.
        /// </typeparam>
        public async Task<IEnumerable<T>> GetDocsFromView<D, V, T>() where D : DesignDocument where V : View where T : CouchDocument, new() {
            return await GetDocsFromView<T>(DesignDocument.GetDesignDocumentName<D>(), View.GetViewName(typeof(V)));
        }
    }

    /// <summary>
    /// Couch.
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

