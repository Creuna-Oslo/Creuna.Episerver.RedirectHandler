using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using System.Linq;
using Castle.Core.Internal;
using Creuna.Episerver.RedirectHandler.Core.Data;
using EPiServer.Logging.Compatibility;

namespace Creuna.Episerver.RedirectHandler.Core.CustomRedirects
{
    /// <summary>
    ///     A collection of custom urls
    /// </summary>
    public class CustomRedirectCollection : CollectionBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(CustomRedirectCollection));

        /// <summary>
        ///     Hashtable for quick lookup of old urls
        /// </summary>
        private readonly Dictionary<string, CustomRedirect> _quickLookupTable;

        public CustomRedirectCollection()
        {
            // Create case insensitive hash table
            _quickLookupTable = new Dictionary<string, CustomRedirect>(StringComparer.InvariantCultureIgnoreCase);
        }

        #region Add

        public int Add(CustomRedirect customRedirect)
        {
            AddLookup(customRedirect);

            return List.Add(customRedirect);
        }

        private void AddLookup(/*[NotNull]*/ CustomRedirect customRedirect)
        {
            if (customRedirect == null) throw new ArgumentNullException(nameof(customRedirect));

            var lookupKey = GetLookupKey(customRedirect);

            if (_quickLookupTable.ContainsKey(lookupKey))
            {
                Log.WarnFormat("Two or more redirects set up for Old Url: {0}", customRedirect.OldUrl);
            }
            else
            {
                _quickLookupTable[lookupKey] = customRedirect;
            }
        }

        private void RemoveLookup(/*[NotNull]*/ CustomRedirect customRedirect)
        {
            _quickLookupTable.Remove(GetLookupKey(customRedirect));
        }

        #endregion

        #region Insert

        public void Insert(int index, CustomRedirect customRedirect)
        {
            AddLookup(customRedirect);
            List.Insert(index, customRedirect);
        }

        #endregion

        #region Remove

        public void Remove(CustomRedirect customRedirect)
        {
            RemoveLookup(customRedirect);
            List.Remove(customRedirect);
        }

        #endregion

        #region Find

        private static string GetQueryFrom(Uri uri)
        {
            return uri.IsAbsoluteUri ? GetQuerystringWithoutQuestionMark(uri.Query) : GetQuerystringWithoutQuestionMark(GetQueryFromLocalUri(uri));
        }

        private static string GetQueryFromLocalUri(Uri localUri)
        {
            var queryStart = localUri.OriginalString.IndexOf('?');
            if (queryStart > -1)
                return localUri.OriginalString.Substring(queryStart + 1);
            return string.Empty;
        }

        private static string GetQuerystringWithoutQuestionMark(string querystring)
        {
            return querystring.StartsWith("?", StringComparison.OrdinalIgnoreCase) ? querystring.Substring(1) : querystring;
        }

        #endregion

        #region Contains

        // TODO: If you changed the parameters to Find (above), change them here as well.
        public bool Contains(string oldUrl)
        {
            return _quickLookupTable.ContainsKey(GetLookupKey(oldUrl));
        }

        #endregion

        #region this[int aIndex]

        public CustomRedirect this[int index]
        {
            get { return (CustomRedirect)List[index]; }
            set { List[index] = value; }
        }

        #endregion

        /*[CanBeNull]*/
        public CustomRedirect Find(/*[NotNull]*/ Uri urlNotFound)
        {
            if (urlNotFound == null) throw new ArgumentNullException(nameof(urlNotFound));

            var urlsToSearch = new[]
            {
                // Handle absolute addresses first
                urlNotFound.AbsoluteUri,
                // and its protocol invariant version
                RemoveProtocol(urlNotFound.AbsoluteUri),
                // then try it without query
                RemoveQuery(urlNotFound.AbsoluteUri),
                // and the protocol invariant version
                RemoveProtocol(RemoveQuery(urlNotFound.AbsoluteUri)),
                // common case 
                urlNotFound.PathAndQuery,
                // and the same without query
                RemoveQuery(urlNotFound.PathAndQuery),
                // Handle legacy databases with encoded values
                HttpUtility.HtmlEncode(urlNotFound.PathAndQuery),
                // and the same without query
                HttpUtility.HtmlEncode(RemoveQuery(urlNotFound.PathAndQuery))
            };

            var foundRedirect = TryFind(urlsToSearch);
            var result = PostProcessRedirect(urlNotFound, foundRedirect);
            return result;
        }

        /*[NotNull]*/
        string RemoveProtocol(/*[NotNull]*/ string url)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));

            const string http = "http:";

            if (url.StartsWith(http))
            {
                return url.Substring(http.Length);
            }

            const string https = "https:";

            if (url.StartsWith(https))
            {
                return url.Substring(https.Length);
            }

            return url;
        }

        /*[NotNull]*/
        string RemoveQuery(/*[NotNull]*/ string url)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));
            var queryStartsAt = url.IndexOf("?", StringComparison.InvariantCultureIgnoreCase);

            var result = queryStartsAt > -1 ? url.Substring(0, queryStartsAt) : url;
            return result;
        }

        /*[CanBeNull]*/
        private CustomRedirect TryFind(/*[CanBeNull]*/ params string[] urls)
        {
            foreach (var url in urls.Distinct(StringComparer.InvariantCultureIgnoreCase))
            {
                var foundRedirect = FindInternal(url);
                if (foundRedirect != null)
                    return foundRedirect;
            }

            return null;
        }

        private CustomRedirect FindInternal(string url)
        {
            var foundRedirect = TryLookup(url);

            if (foundRedirect != null)
            {
                return foundRedirect;
            }

            // No exact match could be done, so we'll check if the 404 url
            // starts with one of the urls we're matching against. This
            // will be kind of a wild card match (even though we only check
            // for the start of the url
            // Example: http://www.mysite.com/news/mynews.html is not found
            // We have defined an "<old>/news</old>" entry in the config
            // file. We will get a match on the /news part of /news/myne...
            // Depending on the skip wild card append setting, we will either
            // redirect using the <new> url as is, or we'll append the 404
            // url to the <new> url.

            foreach (var pair in _quickLookupTable)
            {
                var redirect = pair.Value;
                if (redirect.ExactMatch)
                {
                    continue; // todo: [low] performance issue: we can store only non-exact-mathces separately
                }

                // See if this "old" url (the one that cannot be found) starts with one 
                if (url.StartsWith(pair.Key, StringComparison.InvariantCultureIgnoreCase))
                {
                    foundRedirect = redirect;
                    if (redirect.State == GetState.Ignored)
                    {
                        return null;
                    }

                    return redirect;
                }
            }
            return null;
        }

        /*[CanBeNull]*/
        private CustomRedirect TryLookup(/*[NotNull]*/ string url)
        {
            CustomRedirect foundRedirect = null;
            _quickLookupTable.TryGetValue(GetLookupKey(url), out foundRedirect);
            return foundRedirect;
        }

        /*[CanBeNull]*/
        private string GetLookupKey(/*[NotNull]*/ CustomRedirect customRedirect)
        {
            if (customRedirect == null) throw new ArgumentNullException(nameof(customRedirect));
            return GetLookupKey(customRedirect.OldUrl);
        }

        /*[CanBeNull]*/
        private string GetLookupKey(/*[CanBeNull]*/ string url)
        {
            return UrlStandardizer.Standardize(url);
        }

        /*[CanBeNull]*/
        private CustomRedirect PostProcessRedirect(/*[NotNull]*/Uri urlNotFound, /*[CanBeNull]*/ CustomRedirect redirect)
        {
            if (urlNotFound == null) throw new ArgumentNullException(nameof(urlNotFound));
            if (redirect == null)
                return null;

            redirect = ProcessRedirect(urlNotFound, redirect);

            return redirect;
        }

        private CustomRedirect ProcessRedirect(/*[NotNull]*/Uri urlNotFound, /*[CanBeNull]*/ CustomRedirect redirect)
        {
            var redirCopy = new CustomRedirect(redirect);

            var newUrl = redirCopy.NewUrl;

            if (redirCopy.AppendMatchToNewUrl)
            {
                newUrl = AppendMatchToNewUrl(urlNotFound, redirCopy);

                redirCopy.NewUrl = newUrl;
            }

            if (redirCopy.IncludeQueryString)
            {
                newUrl = AddQueryStringToUrl(newUrl, urlNotFound.Query);
            }

            redirCopy.NewUrl = newUrl;

            return redirCopy;
        }

        private string AddQueryStringToUrl(string url, string queryString)
        {
            if (string.IsNullOrEmpty(queryString))
            {
                return url;
            }

            var newUrlParts = url.Split(new[] { "?" }, StringSplitOptions.RemoveEmptyEntries);
            var newUrlQuery = newUrlParts.Length > 1 ? newUrlParts[1] : null;

            var originalQueryParsed = HttpUtility.ParseQueryString(queryString);
            var targetQueryParsed = newUrlQuery != null
                ? HttpUtility.ParseQueryString(newUrlQuery)
                : new NameValueCollection();

            var appendQuery = Merge(targetQueryParsed, originalQueryParsed);

            var newUrl = url;

            if (!appendQuery.IsNullOrEmpty())
            {
                newUrl = newUrl.Contains("?") ? $"{newUrl}&{appendQuery}" : $"{newUrl}?{appendQuery}";
            }

            return newUrl;
        }

        private string GetAppend( /*[NotNull]*/ Uri urlNotFound, /*[NotNull]*/CustomRedirect redirect)
        {
            var url = urlNotFound.ToString();

            var urlFromRule = UrlStandardizer.Standardize(redirect.OldUrl);

            var idx = url.IndexOf(urlFromRule, StringComparison.Ordinal) + urlFromRule.Length;

            if (idx > 0 && url.Length > idx)
            {
                var append = url.Substring(idx);

                return append;
            }

            return string.Empty;
        }

        private string AppendMatchToNewUrl(/*[NotNull]*/Uri urlNotFound, /*[NotNull]*/CustomRedirect redirect)
        {
            // We need to append the 404 to the end of the
            // new one. Make a copy of the redir object as we
            // are changing it.
            var append = GetAppend(urlNotFound, redirect);

            if (append != string.Empty && urlNotFound.Query != string.Empty)
            {
                if (append.Contains(urlNotFound.Query))
                {
                    append = append.Replace(urlNotFound.Query, string.Empty);
                }
            }

            var redirCopy = new CustomRedirect(redirect);

            if (append != string.Empty && append != "/")
            {
                var redirectUrl = new Uri(redirCopy.NewUrl, IsAbsoluteUrl(redirCopy.NewUrl) ? UriKind.Absolute : UriKind.Relative);
                var redirectUrlQuery = GetQueryFrom(redirectUrl);

                var newUrl = UrlStandardizer.Standardize(RemoveQuery(redirCopy.NewUrl));

                newUrl = UrlStandardizer.Standardize(newUrl + append);

                if (redirectUrlQuery != string.Empty)
                {
                    newUrl = AddQueryStringToUrl(newUrl, redirectUrlQuery);
                }

                return newUrl;
            }

            return redirect.NewUrl;
        }

        /*[NotNull]*/
        private string Merge(/*[NotNull]*/ NameValueCollection targetQuery, /*[NotNull]*/ NameValueCollection originalQuery)
        {
            if (targetQuery == null) throw new ArgumentNullException(nameof(targetQuery));
            if (originalQuery == null) throw new ArgumentNullException(nameof(originalQuery));

            var appendQueryArray = originalQuery.AllKeys.Where(x => targetQuery[x] == null)
                .Select(x => $"{HttpUtility.UrlEncode(x)}={HttpUtility.UrlEncode(originalQuery[x])}").ToArray();
            var query = string.Join("&", appendQueryArray);
            return query;
        }

        private bool IsAbsoluteUrl(/*[NotNull]*/ string url)
        {
            if (url == null) throw new ArgumentNullException(nameof(url));

            return url.StartsWith("//", StringComparison.InvariantCultureIgnoreCase)
                   || url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase)
                   || url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}