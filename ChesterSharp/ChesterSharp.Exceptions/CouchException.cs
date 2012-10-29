using System;
using System.Net;

namespace ChesterSharp.Exceptions
{
    public class CouchException : Exception {
        public HttpStatusCode StatusCode { get; set; }
        
        public CouchException(string message, HttpStatusCode statusCode) : base(string.Format("[CouchException: StatusCode={0}, Message: {1}]", statusCode, message)) {
        }
    }
}
