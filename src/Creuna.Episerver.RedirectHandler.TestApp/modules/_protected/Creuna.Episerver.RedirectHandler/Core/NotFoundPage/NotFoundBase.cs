using System;
using System.Reflection;
using System.Web;
using System.Web.UI;
using EPiServer.Logging.Compatibility;

namespace Creuna.Episerver.RedirectHandler.Core.NotFoundPage
{
    public class NotFoundBase : Page
    {
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private PageContent _content;
        private string _referer;
        private Uri _urlNotFound;

        /// <summary>
        ///     Content for the page
        /// </summary>
        /// <value></value>
        public PageContent Content
        {
            get
            {
                if (_content == null)
                    _content = NotFoundPageUtil.Get404PageLanguageResourceContent();
                return _content;
            }
        }

        /// <summary>
        ///     Holds the url - if any - the user tried to find
        /// </summary>
        public Uri UrlNotFound
        {
            get
            {
                if (_urlNotFound == null)
                {
                    _urlNotFound = NotFoundPageUtil.GetUrlNotFound(Page);
                }
                return _urlNotFound;
            }
        }

        /// <summary>
        ///     The refering url
        /// </summary>
        public string Referer
        {
            get
            {
                if (_referer == null)
                {
                    _referer = HttpUtility.HtmlEncode(NotFoundPageUtil.GetReferer(Page));
                }
                return _referer;
            }
        }

        /// <summary>
        ///     Load event for the page
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            NotFoundPageUtil.HandleOnLoad(Page, UrlNotFound, Referer);
        }
    }
}