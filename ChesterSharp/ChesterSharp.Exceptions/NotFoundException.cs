using System;
using System.Net;

namespace ChesterSharp.Exceptions
{
    public class NotFoundException : CouchException {
        public NotFoundException(string message, HttpStatusCode statusCode) : base(message, statusCode) {
        }
    }
}

