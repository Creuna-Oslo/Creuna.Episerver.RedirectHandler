using System;
using Creuna.Episerver.RedirectHandler.Core.Data;
using EPiServer.Data;
using EPiServer.Data.Dynamic;

namespace Creuna.Episerver.RedirectHandler.Core.CustomRedirects
{
    [EPiServerDataStore(AutomaticallyRemapStore = true, StoreName = "Creuna.Episerver.RedirectHandler.CustomRedirect")]
    public class CustomRedirect : IDynamicData
    {
        public CustomRedirect()
        {

        }

        public CustomRedirect(string oldUrl, string newUrl, bool appendMatchToNewUrl, bool exactMatch, bool includeQueryString)
            : this(oldUrl, newUrl, appendMatchToNewUrl, exactMatch, includeQueryString, 0)
        { }

        public CustomRedirect(string oldUrl, string newUrl, bool appendMatchToNewUrl, bool exactMatch,
            bool includeQueryString, int state)
            : this(oldUrl, newUrl, appendMatchToNewUrl, exactMatch, includeQueryString, state, 0)
        {

        }

        public CustomRedirect(string oldUrl, string newUrl, bool appendMatchToNewUrl, bool exactMatch, bool includeQueryString, int state, int notFoundErrorCount)
        {
            OldUrl = (oldUrl ?? "").ToLower().Trim();
            NewUrl = (newUrl ?? "").ToLower().Trim();
            AppendMatchToNewUrl = appendMatchToNewUrl;
            ExactMatch = exactMatch;
            IncludeQueryString = includeQueryString;
            State = state;
            NotfoundErrorCount = notFoundErrorCount;
        }

        public CustomRedirect(CustomRedirect redirect)
        {
            OldUrl = redirect.OldUrl;
            NewUrl = redirect.NewUrl;
            AppendMatchToNewUrl = redirect.AppendMatchToNewUrl;
            ExactMatch = redirect.ExactMatch;
            IncludeQueryString = redirect.IncludeQueryString;
        }

        public string NewUrl { get; set; }
        public string OldUrl { get; set; }
        public bool AppendMatchToNewUrl { get; set; }
        public bool ExactMatch { get; set; }
        public bool IncludeQueryString { get; set; }
        public int State { get; set; }
        public int NotfoundErrorCount { get; set; }

        /// <summary>
        ///     Tells if the new url is a virtual url, not containing
        ///     the base root url to redirect to. All urls starting with
        ///     "/" is determined to be virtuals.
        /// </summary>
        public bool IsVirtual
        {
            get
            {
                return NewUrl.StartsWith("/");
            }
        }

        public Identity Id { get; set; }

        /// <summary>
        ///     The hash code for the CustomRedirect class is the
        ///     old url string, which is the one we'll be doing lookups
        ///     based on.
        /// </summary>
        /// <returns>The Hash code of the old Url</returns>
        public override int GetHashCode()
        {
            return OldUrl.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj.GetType() != GetType())
                return false;
            return OldUrl == null
                   || OldUrl.Equals(((CustomRedirect)obj).OldUrl);
        }

        public static CustomRedirect CreateIgnored(string oldUrl)
        {
            return new CustomRedirect(oldUrl, string.Empty, false, true, true, Convert.ToInt32(DataStoreHandler.GetState.Ignored));
        }

        public CustomRedirect WithNewUrl(string newUrl)
        {
            return new CustomRedirect(
                OldUrl, newUrl, AppendMatchToNewUrl, ExactMatch, IncludeQueryString, State);
        }

        public CustomRedirect WithNotFoundErrorCount(int count)
        {
            return new CustomRedirect(
                OldUrl, NewUrl, AppendMatchToNewUrl, ExactMatch, IncludeQueryString, State, count);
        }
    }
}