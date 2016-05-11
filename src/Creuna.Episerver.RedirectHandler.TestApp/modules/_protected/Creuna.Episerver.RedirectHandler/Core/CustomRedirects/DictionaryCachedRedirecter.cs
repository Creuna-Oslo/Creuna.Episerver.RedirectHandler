using System;
using System.Collections.Generic;
using Creuna.Episerver.RedirectHandler.Core.Logging;

namespace Creuna.Episerver.RedirectHandler.Core.CustomRedirects
{
    public class DictionaryCachedRedirecter : ICachedRedirecter
    {
        private readonly IRedirecter _redirecter;
        private readonly Dictionary<string, RedirectAttempt> _redirects
            = new Dictionary<string, RedirectAttempt>(StringComparer.OrdinalIgnoreCase);
        private readonly object _lockObject = new object();

        public DictionaryCachedRedirecter(IRedirecter redirecter)
        {
            _redirecter = redirecter;
        }

        public RedirectAttempt Redirect(string referer, Uri urlNotFound)
        {
            bool added = false;
            if (!_redirects.ContainsKey(urlNotFound.PathAndQuery))
            {
                lock (_lockObject)
                {
                    if (!_redirects.ContainsKey(urlNotFound.PathAndQuery))
                    {
                        _redirects.Add(urlNotFound.PathAndQuery, _redirecter.Redirect(referer, urlNotFound));
                        added = true;
                    }
                }
            }
            var redirect = _redirects[urlNotFound.PathAndQuery];
            if (!(redirect.Redirected || added))
                RequestLogger.Instance.LogRequest(urlNotFound.PathAndQuery, referer);
            return redirect;
        }

        public long GetCachedRedirectsCount()
        {
            return _redirects.Count;
        }

        public void ClearCache()
        {
            _redirects.Clear();
        }
    }
}
