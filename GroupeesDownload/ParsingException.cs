using System;
using System.Collections.Generic;
using System.Text;

namespace GroupeesDownload
{

    [Serializable]
    public class ParsingException : Exception
    {
        public string Html { get; }

        public ParsingException() { }
        public ParsingException(string message) : base(message) { }
        public ParsingException(string message, string html) : this(message)
        {
            Html = html;
        }
        public ParsingException(string message, Exception inner) : base(message, inner) { }
        public ParsingException(string message, string html, Exception inner) : this(message, inner)
        {
            Html = html;
        }
        protected ParsingException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
