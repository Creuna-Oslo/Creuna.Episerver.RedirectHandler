using System;
using System.Collections.Generic;
using System.Web;
using Creuna.Episerver.RedirectHandler.Core.Configuration;
using Creuna.Episerver.RedirectHandler.Core.Data;
using Creuna.Episerver.RedirectHandler.Core.Logging;

namespace Creuna.Episerver.RedirectHandler.Core.CustomRedirects
{
    public class Redirecter : IRedirecter
    {
        private readonly CustomRedirectCollection _customRedirects;
        private readonly RedirectConfiguration _redirectConfiguration;

        public Redirecter(CustomRedirectCollection customRedirects, RedirectConfiguration redirectConfiguration)
        {
            _customRedirects = customRedirects;
            _redirectConfiguration = redirectConfiguration;
        }

        public RedirectAttempt Redirect(string referer, Uri urlNotFound)
        {
            var redirect = FindRedirect(urlNotFound);

            string pathAndQuery = Uri.UnescapeDataString(urlNotFound.PathAndQuery);
            if (redirect != null)
            {
                if (redirect.State.Equals((int)DataStoreHandler.GetState.Saved))
                {
                    // Found it, however, we need to make sure we're not running in an
                    // infinite loop. The new url must not be the referrer to this page
                    if (string.Compare(redirect.NewUrl, pathAndQuery, StringComparison.InvariantCultureIgnoreCase) != 0)
                        return RedirectAttempt.Success(redirect.NewUrl);
                }
            }
            else
            {
                // log request to database - if logging is turned on.
                if (_redirectConfiguration.Logging == LoggerMode.On)
                    RequestLogger.Instance.LogRequest(pathAndQuery, referer);
            }
            return RedirectAttempt.Miss;
        }

        private CustomRedirect FindRedirect(Uri urlNotFound)
        {
            var touchedRedirects = new Dictionary<CustomRedirect, List<string>>();
            var redirect = GetRedirect(urlNotFound);
            CustomRedirect previousRedirect = null;
            while (redirect != null)
            {
                if (touchedRedirects.ContainsKey(redirect) && touchedRedirects[redirect].Contains(redirect.NewUrl))
                    throw new RedirectLoopException();
                AddUrlToTouchedRedirects(touchedRedirects, redirect);
                previousRedirect = redirect;
                var urlToRedirect = urlNotFound.GetLeftPart(UriPartial.Authority);
                var redirectTargetUri = new Uri(previousRedirect.NewUrl, UriKind.RelativeOrAbsolute);
                if (redirectTargetUri.IsAbsoluteUri)
                    redirect = null;
                else
                    redirect =
                        GetRedirect(
                            new Uri(new Uri(urlToRedirect, UriKind.Absolute), new Uri(previousRedirect.NewUrl, UriKind.Relative)));
            }
            redirect = previousRedirect;
            return redirect;
        }

        private CustomRedirect GetRedirect(Uri urlNotFound)
        {
            return _customRedirects.Find(urlNotFound);
        }

        private static void AddUrlToTouchedRedirects(Dictionary<CustomRedirect, List<string>> touchedRedirects, CustomRedirect redirect)
        {
            if (!touchedRedirects.ContainsKey(redirect))
                touchedRedirects.Add(redirect, new List<string>());
            touchedRedirects[redirect].Add(redirect.NewUrl);
        }
    }
}