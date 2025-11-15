using System;
using System.Collections.Generic;
using System.Text;

namespace Contigo
{
    public class FacebookException : Exception
    {
        internal FacebookException(string message, Exception e)
            : base(message, e)
        { }

        internal FacebookException(string errorXml, int errorCode, string message, string requestXml)
            : base(message)
        {
            ErrorXml = errorXml;
            ErrorCode = errorCode;
            RequestXml = requestXml;
        }

        public int ErrorCode { get; private set; }

        public string ErrorXml { get; private set; }

        public string RequestXml { get; private set; }
    } 
}
