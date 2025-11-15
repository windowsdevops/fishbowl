
namespace Contigo
{
    using System;

    public class FacebookImageLink
    {
        internal FacebookImageLink()
        {}

        public Uri Link { get; internal set; }

        public FacebookImage Image { get; internal set; }
    }
}