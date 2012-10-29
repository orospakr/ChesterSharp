using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ChesterSharp.Documents
{
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
}

