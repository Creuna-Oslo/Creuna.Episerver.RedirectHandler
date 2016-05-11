using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Web;
using System.Web.Mvc;
using Creuna.Episerver.RedirectHandler.Core.CustomRedirects;
using Creuna.Episerver.RedirectHandler.Core.Data;
using Creuna.Episerver.RedirectHandler.Models;
using EPiServer.Editor.TinyMCE.Plugins;
using EPiServer.Framework.Localization;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.Shell.Gadgets;
using EPiServer.Shell.Web;

namespace Creuna.Episerver.RedirectHandler.Controllers
{
    [ScriptResource("ClientResources/Scripts/jquery.blockUI.js")]
    [Gadget(ResourceType = typeof(NotFoundRedirectController),
        NameResourceKey = "GadgetName", DescriptionResourceKey = "GadgetDescription")]
    [CssResource("ClientResources/Content/RedirectGadget.css")]
    [ScriptResource("ClientResources/Scripts/jquery.form.js")]
    [Authorize]
    public class NotFoundRedirectController : Controller
    {
        private readonly CustomRedirectHandler _customRedirectHandler;
        private readonly DataStoreHandler _dataStoreHandler;
        private static readonly ILogger Logger = LogManager.GetLogger();

        public NotFoundRedirectController(CustomRedirectHandler customRedirectHandler, DataStoreHandler dataStoreHandler)
        {
            _customRedirectHandler = customRedirectHandler;
            _dataStoreHandler = dataStoreHandler;
        }

        public static string GadgetEditMenuName
        {
            get { return LocalizationService.Current.GetString("/gadget/redirects/configure"); }
        }

        public static string GadgetName
        {
            get { return LocalizationService.Current.GetString("/gadget/redirects/name"); }
        }

        public static string GadgetDescription
        {
            get { return LocalizationService.Current.GetString("/gadget/redirects/description"); }
        }

        private void CheckAccess()
        {
            if (!PrincipalInfo.HasEditAccess)
            {
                throw new SecurityException("Access denied");
            }
        }

        public ActionResult Index(int? pageNumber, string searchWord, int? pageSize, bool? isSuggestions)
        {
            CheckAccess();

            if (!string.IsNullOrEmpty(CustomRedirectHandler.CustomRedirectHandlerException))
            {
                return
                    Content("An error has occured in the dynamic data store: " +
                            CustomRedirectHandler.CustomRedirectHandlerException);
            }
            bool suggestion = false;

            List<CustomRedirect> customRedirectList;
            if (isSuggestions.HasValue && isSuggestions.Value)
            {
                customRedirectList = GetSuggestions().ToList();

                suggestion = true;
                RedirectIndexViewData viewData = GetRedirectIndexViewData(pageNumber, customRedirectList,
                    GetSearchResultInfo(searchWord, customRedirectList.Count, suggestion), searchWord, pageSize,
                    suggestion);
                if (customRedirectList != null && customRedirectList.Count > 0)
                {
                    viewData.HighestSuggestionValue = customRedirectList.First().NotfoundErrorCount;
                    viewData.LowestSuggestionValue = customRedirectList.Last().NotfoundErrorCount;
                }
                return View("Index", viewData);
            }
            customRedirectList = GetData(searchWord);
            return View("Index",
                GetRedirectIndexViewData(pageNumber, customRedirectList,
                    GetSearchResultInfo(searchWord, customRedirectList.Count, suggestion), searchWord, pageSize,
                    suggestion));
        }

        public ActionResult SaveSuggestion(string oldUrl, string newUrl, string exactMatch, string includeQuerystring, string skipWildCardAppend, int? pageNumber,
            int? pageSize)
        {
            CheckAccess();
            SaveRedirect(oldUrl, newUrl, skipWildCardAppend, exactMatch, includeQuerystring);

            // delete rows from DB
            DataAccessBaseEx dbAccess = DataAccessBaseEx.GetWorker();
            dbAccess.DeleteRowsForRequest(oldUrl);

            //
            List<CustomRedirect> customRedirectList = GetSuggestions().ToList();
            string actionInfo = string.Format(LocalizationService.Current.GetString("/gadget/redirects/saveredirect"),
                oldUrl, newUrl);
            DataStoreEventHandlerHook.DataStoreUpdated();
            RedirectIndexViewData viewData = GetRedirectIndexViewData(pageNumber, customRedirectList, actionInfo, null,
                pageSize, true);
            viewData.HighestSuggestionValue = customRedirectList.First().NotfoundErrorCount;
            viewData.LowestSuggestionValue = customRedirectList.Last().NotfoundErrorCount;
            return View("Index", viewData);
        }

        public ActionResult Suggestions()
        {
            CheckAccess();
            return Index(null, "", null, true);
        }

        [GadgetAction(Text = "Administer")]
        public ActionResult Administer()
        {
            CheckAccess();
            return View();
        }

        [ValidateInput(false)]
        public ActionResult Save(string oldUrl, string newUrl, string append, string exactMatch, string includeQuerystring, int? pageNumber, int? pageSize)
        {
            CheckAccess();
            SaveRedirect(oldUrl, newUrl, append, exactMatch, includeQuerystring);
            List<CustomRedirect> redirectList = GetData(null);
            string actionInfo = string.Format(LocalizationService.Current.GetString("/gadget/redirects/saveredirect"),
                oldUrl, newUrl);
            return View("Index", GetRedirectIndexViewData(pageNumber, redirectList, actionInfo, null, pageSize, false));
        }

        public void SaveRedirect(string oldUrl, string newUrl, string skipWildCardAppend, string exactMatch, string includeQuerystring)
        {
            Logger.Debug("Adding redirect: '{0}' -> '{1}'", oldUrl, newUrl);
            // Get hold of the datastore
            _dataStoreHandler.SaveCustomRedirect(
                new CustomRedirect(oldUrl.Trim(), newUrl.Trim(),
                skipWildCardAppend != null,
                exactMatch != null,
                includeQuerystring != null,
                0));
            DataStoreEventHandlerHook.DataStoreUpdated();
        }

        public ActionResult IgnoreRedirect(string oldUrl, int pageNumber, string searchWord, int pageSize)
        {
            CheckAccess();
            // delete rows from DB
            DataAccessBaseEx dbAccess = DataAccessBaseEx.GetWorker();
            dbAccess.DeleteRowsForRequest(oldUrl);

            // add redirect to dds with state "ignored"
            var redirect = CustomRedirect.CreateIgnored(oldUrl);
            _dataStoreHandler.SaveCustomRedirect(redirect);
            DataStoreEventHandlerHook.DataStoreUpdated();

            List<CustomRedirect> customRedirectList = GetSuggestions().ToList();
            string actionInfo = string.Format(
                LocalizationService.Current.GetString("/gadget/redirects/ignoreredirect"), oldUrl);
            RedirectIndexViewData viewData = GetRedirectIndexViewData(pageNumber, customRedirectList, actionInfo,
                searchWord, pageSize, true);
            viewData.HighestSuggestionValue = customRedirectList.First().NotfoundErrorCount;
            viewData.LowestSuggestionValue = customRedirectList.Last().NotfoundErrorCount;
            return View("Index", viewData);
        }

        [ValidateInput(false)]
        public ActionResult Delete(string oldUrl, int? pageNumber, string searchWord, int? pageSize)
        {
            CheckAccess();

            Logger.Debug("Deleting redirect: '{0}'", oldUrl);

            _dataStoreHandler.DeleteCustomRedirect(oldUrl);
            DataStoreEventHandlerHook.DataStoreUpdated();
            List<CustomRedirect> customRedirectList = GetData(searchWord);
            //Make sure that the searchinfo is contained after an item has been deleted - if there is any.
            return View("Index",
                GetRedirectIndexViewData(pageNumber, customRedirectList,
                    GetSearchResultInfo(searchWord, customRedirectList.Count, false), searchWord, pageSize, false));
        }

        /// <summary>
        ///     Get the data that will be presented in the view(s).
        /// </summary>
        /// <param name="pageNumber">The current page number for the pager view</param>
        /// <param name="redirectList">The List of redirects</param>
        /// <param name="actionInformation">Text that will be presented in the view</param>
        /// <returns></returns>
        public RedirectIndexViewData GetRedirectIndexViewData(int? pageNumber, List<CustomRedirect> redirectList,
            string actionInformation, string searchWord, int? pageSize, bool isSuggestions)
        {
            var indexData = new RedirectIndexViewData();
            indexData.IsSuggestions = isSuggestions;
            indexData.ActionInformation = actionInformation;
            indexData.SearchWord = searchWord;
            indexData.TotalItemsCount = redirectList.Count;
            indexData.PageNumber = pageNumber ?? 1;
            //TODO: read pagersize and pagesize from configuration.
            indexData.PagerSize = 4;
            indexData.PageSize = pageSize ?? 30;
            if (redirectList.Count > indexData.PageSize)
                indexData.CustomRedirectList = redirectList.GetRange(indexData.MinIndexOfItem - 1,
                    indexData.MaxIndexOfItem - indexData.MinIndexOfItem + 1);
            else
                indexData.CustomRedirectList = redirectList;
            return indexData;
        }

        public ActionResult Ignored()
        {
            CheckAccess();
            List<CustomRedirect> ignoredRedirects = _dataStoreHandler.GetIgnoredRedirect();
            return View("Ignored", ignoredRedirects);
        }


        public ActionResult Unignore(string url)
        {
            CheckAccess();
            _dataStoreHandler.DeleteCustomRedirect(url);
            return Ignored();
        }

        public ActionResult Referers(string url)
        {
            CheckAccess();
            Dictionary<string, int> referers = DataHandler.GetReferers(url);
            ViewData.Add("refererUrl", url);
            return View("Referers", referers);
        }

        public ActionResult DeleteAllIgnored()
        {
            CheckAccess();
            int deleteCount = _dataStoreHandler.DeleteAllIgnoredRedirects();
            string infoText = string.Format(LocalizationService.Current.GetString("/gadget/redirects/ignoredremoved"),
                deleteCount);
            ViewData["information"] = infoText;
            return View("Administer");
        }

        public ActionResult DeleteAllSuggestions()
        {
            CheckAccess();
            DataAccessBaseEx.GetWorker().DeleteAllSuggestions();
            ViewData["information"] = LocalizationService.Current.GetString("/gadget/redirects/suggestionsdeleted");
            return View("Administer");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public FileUploadJsonResult ImportRedirects(HttpPostedFileBase xmlfile)
        {
            CheckAccess();
            // Read all redirects from xml file
            var parser = new RedirectsXmlParser(xmlfile.InputStream);
            // Save all redirects from xml file
            CustomRedirectCollection redirects = (parser.Load() ?? new CustomRedirectCollection());
            string message;
            if (redirects.Count > 0)
            {
                _customRedirectHandler.SaveCustomRedirects(redirects);
                message = string.Format(LocalizationService.Current.GetString("/gadget/redirects/importsuccess"),
                    redirects.Count);
            }
            else
            {
                message = LocalizationService.Current.GetString("/gadget/redirects/importnone");
            }
            return new FileUploadJsonResult { Data = new { message } };
        }


        public ActionResult DeleteSuggestions(int maxErrors, int minimumDays)
        {
            CheckAccess();
            DataAccessBaseEx.GetWorker().DeleteSuggestions(maxErrors, minimumDays);
            ViewData["information"] = LocalizationService.Current.GetString("/gadget/redirects/suggestionsdeleted");
            return View("Administer");
        }

        /// <summary>
        ///     Get the tekst that will be displayed in the info area of the gadget.
        /// </summary>
        /// <param name="searchWord"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public string GetSearchResultInfo(string searchWord, int count, bool isSuggestions)
        {
            string actionInfo;
            if (string.IsNullOrEmpty(searchWord) && !isSuggestions)
            {
                actionInfo = string.Format(LocalizationService.Current.GetString("/gadget/redirects/storedredirects"),
                    count);
                actionInfo += " " +
                              string.Format(LocalizationService.Current.GetString("/gadget/redirects/andsuggestions"),
                                  DataHandler.GetTotalSuggestionCount());
            }
            else if (string.IsNullOrEmpty(searchWord) && isSuggestions)
                actionInfo = string.Format(
                    LocalizationService.Current.GetString("/gadget/redirects/storedsuggestions"), count);
            else if (isSuggestions)
                actionInfo = string.Format(
                    LocalizationService.Current.GetString("/gadget/redirects/searchsuggestions"), searchWord, count);
            else
                actionInfo = string.Format(LocalizationService.Current.GetString("/gadget/redirects/searchresult"),
                    searchWord, count);
            return actionInfo;
        }

        /// <summary>
        ///     Get custom redirect data from dynamic data store.
        /// </summary>
        /// <param name="searchWord"></param>
        /// <returns></returns>
        public List<CustomRedirect> GetData(String searchWord)
        {
            return string.IsNullOrEmpty(searchWord)
                ? _dataStoreHandler.GetCustomRedirects(true)
                : _dataStoreHandler.SearchCustomRedirects(searchWord);
        }

        public IEnumerable<CustomRedirect> GetSuggestions()
        {
            return DataHandler.GetRedirects()
                .Select(redirect =>
                new CustomRedirect(
                    redirect.Key,
                    null,
                    false,
                    true,
                    true,
                    Convert.ToInt32(DataStoreHandler.GetState.Suggestion))
                    .WithNotFoundErrorCount(redirect.Value)).ToList();
        }
    }

    public class FileUploadJsonResult : JsonResult
    {
        public override void ExecuteResult(ControllerContext context)
        {
            ContentType = "text/html";
            context.HttpContext.Response.Write("<textarea>");
            base.ExecuteResult(context);
            context.HttpContext.Response.Write("</textarea>");
        }
    }
}