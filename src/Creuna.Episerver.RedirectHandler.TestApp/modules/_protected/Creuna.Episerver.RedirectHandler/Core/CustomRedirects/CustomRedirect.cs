using System;
using Creuna.Episerver.RedirectHandler.Core.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Creuna.Episerver.RedirectHandler.Core.CustomRedirects
{
    public class CustomRedirect
    {
        private string _oldUrl;

        public CustomRedirect()
        {

        }

        public CustomRedirect(string oldUrl, string newUrl, bool appendMatchToNewUrl, bool exactMatch, bool includeQueryString)
         : this(oldUrl, newUrl, appendMatchToNewUrl, exactMatch, includeQueryString, 0)
        { }

        public CustomRedirect(string oldUrl, string newUrl, bool appendMatchToNewUrl, bool exactMatch,
            bool includeQueryString, GetState state)
            : this(oldUrl, newUrl, appendMatchToNewUrl, exactMatch, includeQueryString, state, 0)
        {

        }

        public CustomRedirect(CustomRedirect redirect)
        {
            OldUrl = redirect._oldUrl;
            NewUrl = redirect.NewUrl;
            AppendMatchToNewUrl = redirect.AppendMatchToNewUrl;
            IncludeQueryString = redirect.IncludeQueryString;
            State = redirect.State;
            NotfoundErrorCount = redirect.NotfoundErrorCount;
        }

        public CustomRedirect(string oldUrl, string newUrl, bool appendMatchToNewUrl, bool exactMatch, bool includeQueryString, GetState state, int notFoundErrorCount)
        {
            OldUrl = (oldUrl ?? "").ToLower().Trim();
            NewUrl = (newUrl ?? "").ToLower().Trim();
            AppendMatchToNewUrl = appendMatchToNewUrl;
            ExactMatch = exactMatch;
            IncludeQueryString = includeQueryString;
            State = state;
            NotfoundErrorCount = notFoundErrorCount;
        }

        public static CustomRedirect Copy(CustomRedirect redirect)
        {
            return new CustomRedirect
            {
                OldUrl = redirect.OldUrl,
                NewUrl = redirect.NewUrl,
                AppendMatchToNewUrl = redirect.AppendMatchToNewUrl,
                ExactMatch = redirect.ExactMatch,
                IncludeQueryString = redirect.IncludeQueryString
            };
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }

        public string NewUrl { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string OldUrl
        {
            get => UrlStandardizer.Standardize(_oldUrl);
            set => _oldUrl = UrlStandardizer.Standardize(value);
        }
        public bool AppendMatchToNewUrl { get; set; }
        public bool ExactMatch { get; set; }
        public bool IncludeQueryString { get; set; }
        public GetState State { get; set; }
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
            return new CustomRedirect(oldUrl, string.Empty, false, true, true, GetState.Ignored);
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