using System;
using System.Runtime.Serialization;

namespace Creuna.Episerver.RedirectHandler
{
    public class RedirectLoopException : Exception
    {
        public RedirectLoopException() : base() { }
        public RedirectLoopException(string message) : base(message) { }
        public RedirectLoopException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
