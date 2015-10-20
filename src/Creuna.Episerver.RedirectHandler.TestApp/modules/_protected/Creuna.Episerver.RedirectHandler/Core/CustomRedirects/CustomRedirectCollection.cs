using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Linq;
using log4net;

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
            if (_quickLookupTable.ContainsKey(customRedirect.OldUrl))
                Log.WarnFormat("Two or more redirects set up for Old Url: {0}", customRedirect.OldUrl);
            else
                _quickLookupTable.Add(customRedirect.OldUrl, customRedirect);
            return List.Add(customRedirect);
        }

        #endregion

        #region IndexOf

        public int IndexOf(CustomRedirect customRedirect)
        {
            for (int i = 0; i < List.Count; i++)
                if (this[i] == customRedirect) // Found it
                    return i;
            return -1;
        }

        #endregion

        #region Insert

        public void Insert(int index, CustomRedirect customRedirect)
        {
            _quickLookupTable.Add(customRedirect.OldUrl, customRedirect);
            List.Insert(index, customRedirect);
        }

        #endregion

        #region Remove

        public void Remove(CustomRedirect customRedirect)
        {
            _quickLookupTable.Remove(customRedirect.OldUrl);
            List.Remove(customRedirect);
        }

        #endregion

        #region Find

        // TODO: If desired, change parameters to Find method to search based on a property of CustomRedirect.
        public CustomRedirect Find(Uri urlNotFound)
        {
            if (!urlNotFound.IsAbsoluteUri)
                return null;

            return FindRedirect(urlNotFound, HttpUtility.HtmlEncode(urlNotFound.PathAndQuery))
                   ?? FindRedirect(urlNotFound, HttpUtility.HtmlEncode(urlNotFound.AbsolutePath));
        }

        private CustomRedirect FindRedirect(Uri urlNotFound, string oldUri)
        {
            if (_quickLookupTable.ContainsKey(urlNotFound.AbsoluteUri))
                return BuildNewUrl(_quickLookupTable[urlNotFound.AbsoluteUri], urlNotFound.AbsoluteUri, oldUri, urlNotFound.Query);
            if (_quickLookupTable.ContainsKey(oldUri))
                return BuildNewUrl(_quickLookupTable[oldUri], urlNotFound.AbsoluteUri, oldUri, urlNotFound.Query);

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

            return _quickLookupTable.Keys
                .Where(k => oldUri.StartsWith(k, StringComparison.InvariantCultureIgnoreCase))
                .Select(key => Tuple.Create(key, _quickLookupTable[key]))
                .Where(r => !r.Item2.ExactMatch)
                .Select(r => BuildNewUrl(r.Item2, r.Item1, oldUri, urlNotFound.Query))
                .FirstOrDefault();
        }

        private CustomRedirect BuildNewUrl(
            CustomRedirect customRedirect,
            string oldUri,
            string absolutePath,
            string querystring)
        {
            var newUrl = customRedirect.AppendMatchToNewUrl
                ? AppendMatch(customRedirect, oldUri, absolutePath)
                : customRedirect.NewUrl;

            if (customRedirect.IncludeQueryString && querystring.Length > 1)
                newUrl = CreateUrlWithQuerystring(querystring, newUrl);
            return customRedirect.WithNewUrl(newUrl);
        }

        private static string AppendMatch(CustomRedirect customRedirect, string uriToRedirect, string absolutePath)
        {
            var newUrl = new Uri(customRedirect.NewUrl, UriKind.RelativeOrAbsolute);
            var oldUri = new Uri(uriToRedirect, UriKind.Relative);
            var uriToAppend = GetPathFrom(new Uri(absolutePath.Substring(uriToRedirect.Length), UriKind.RelativeOrAbsolute));
            var querystring = GetCompleteQueryFrom(customRedirect, newUrl, oldUri);
            return GetPathFrom(newUrl) + uriToAppend + (string.IsNullOrWhiteSpace(querystring) ? string.Empty : string.Concat("?", querystring));
        }

        private static string GetCompleteQueryFrom(CustomRedirect redirect, Uri newUrl, Uri oldUri)
        {
            var a = GetQueryFrom(newUrl).Split(new[] { "&" }, StringSplitOptions.RemoveEmptyEntries)
                .Concat((redirect.IncludeQueryString ? GetQueryFrom(oldUri).Substring(1) : "").Split(new[] { "&" },
                    StringSplitOptions.RemoveEmptyEntries));
            return string.Join("&", a);
        }

        private static string GetPathFrom(Uri newUrl)
        {
            return (newUrl.IsAbsoluteUri ? newUrl.AbsolutePath : GetPathFromLocalUri(newUrl));
        }

        private static string GetPathFromLocalUri(Uri localUrl)
        {
            var queryStart = localUrl.OriginalString.IndexOf('?');
            if (queryStart > -1)
                return localUrl.OriginalString.Substring(0, queryStart);
            return localUrl.LocalPath;
        }

        private static string GetQueryFrom(Uri uri)
        {
            return uri.IsAbsoluteUri ? uri.Query.Substring(1) : GetQueryFromLocalUri(uri);
        }

        private static string GetQueryFromLocalUri(Uri localUri)
        {
            var queryStart = localUri.OriginalString.IndexOf('?');
            if (queryStart > -1)
                return localUri.OriginalString.Substring(queryStart + 1);
            return string.Empty;
        }

        private static string CreateUrlWithQuerystring(string querystring, string newUrl)
        {
            if (newUrl.IndexOf("?", StringComparison.OrdinalIgnoreCase) > -1)
                return string.Concat(newUrl, "&", GetQuerystringWithoutQuestionMark(querystring));
            return string.Concat(newUrl, "?", GetQuerystringWithoutQuestionMark(querystring));
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
            return _quickLookupTable.ContainsKey(oldUrl);
        }

        #endregion

        #region this[int aIndex]

        public CustomRedirect this[int index]
        {
            get { return (CustomRedirect)List[index]; }
            set { List[index] = value; }
        }

        #endregion
    }
}