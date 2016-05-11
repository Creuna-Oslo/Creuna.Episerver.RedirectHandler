using System.Globalization;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;

namespace Creuna.Episerver.RedirectHandler.Core.NotFoundPage
{
    /// <summary>
    ///     Summary description for PageContent.
    /// </summary>
    public class PageContent
    {
        private const string DEF_TITLE = "Page Not Found";
        private const string DEF_TOPTEXT = "We're sorry, but the page you requested could not be found<br><br>";
        private const string DEF_LOOKING_FOR = "You were looking for:<br>";
        private const string DEF_CAME_FROM = "<br>You came from:<br>";
        private const string DEF_BOTTOM_TEXT = "<br><br><b>Visit <a href=\"/\">the front page</a><b>";
        private string _bottomText = DEF_BOTTOM_TEXT;
        private string _cameFrom = DEF_CAME_FROM;
        private string _lookingFor = DEF_LOOKING_FOR;

        private string _title = DEF_TITLE;
        private string _topText = DEF_TOPTEXT;

        public PageContent()
        {
            InitializeFromLanguageFile();
        }

        /// <summary>
        ///     Gets or sets the bottom text.
        /// </summary>
        /// <value></value>
        public string BottomText
        {
            get { return _bottomText; }
            set { _bottomText = value; }
        }

        /// <summary>
        ///     Gets or sets the came from text.
        /// </summary>
        /// <value></value>
        public string CameFrom
        {
            get { return _cameFrom; }
            set { _cameFrom = value; }
        }

        /// <summary>
        ///     Gets or sets the looking for text.
        /// </summary>
        /// <value></value>
        public string LookingFor
        {
            get { return _lookingFor; }
            set { _lookingFor = value; }
        }

        /// <summary>
        ///     Gets or sets the top text.
        /// </summary>
        /// <value></value>
        public string TopText
        {
            get { return _topText; }
            set { _topText = value; }
        }

        /// <summary>
        ///     Gets or sets the title.
        /// </summary>
        /// <value></value>
        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }

        /// <summary>
        ///     Initializes text strings from the language file.
        /// </summary>
        private void InitializeFromLanguageFile()
        {
            LocalizationService languageService = LocalizationService.Current;

            CultureInfo cultureInfo = ContentLanguage.PreferredCulture;

            if (cultureInfo != null)
            {
                languageService.TryGetStringByCulture("/templates/notfound/title1", cultureInfo, out _title);
                languageService.TryGetStringByCulture("/templates/notfound/toptext", cultureInfo, out _topText);
                languageService.TryGetStringByCulture("/templates/notfound/lookingfor", cultureInfo, out _lookingFor);
                languageService.TryGetStringByCulture("/templates/notfound/referer", cultureInfo, out _cameFrom);
                languageService.TryGetStringByCulture("/templates/notfound/bottomtext", cultureInfo, out _bottomText);
            }
        }
    }
}