using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using ChesterSharp.Answers;
using ChesterSharp.Exceptions;
using ChesterSharp.Documents;

namespace ChesterSharp
{
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
            var result = JsonConvert.DeserializeObject<DocumentCreationResult>(resultantJson);
            try {
                content.Id = result.Id;
            } catch (NotImplementedException e) {
                // things like design documents have a hardcoded ID and object to having it manually set.
            }
            content.Rev = result.Rev;
            return content;
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
}

