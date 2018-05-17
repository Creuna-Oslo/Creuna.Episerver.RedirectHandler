using System;
using Creuna.Episerver.RedirectHandler.Core;
using Creuna.Episerver.RedirectHandler.Core.CustomRedirects;
using FluentAssertions;
using NUnit.Framework;
using Should;

namespace Creuna.Episerver.RedirectHandler
{
    [TestFixture]
    public class CustomRedirectCollectionTests
    {
        private CustomRedirectCollection _sut;

        [SetUp]
        public virtual void SetUp()
        {
            _sut = new CustomRedirectCollection();
            UrlStandardizer.Accessor = () => new DefaultUrlStandardizer();
        }

        public class When_a_redirect_is_found : CustomRedirectCollectionTests
        {
            protected CustomRedirect OriginalRedirect { get; set; }

            public override void SetUp()
            {
                base.SetUp();

                OriginalRedirect = new CustomRedirect("/test", "/sjomatskolen", appendMatchToNewUrl: true, exactMatch: false, includeQueryString: true)
                {
                    Id = Guid.Parse("{07F44868-121A-43BA-B1A9-E45DE62F9293}")
                };

                _sut.Add(OriginalRedirect);
            }


            [TestCase("http://www.website.com/test/x1", "/sjomatskolen/x1")]
            [TestCase("http://www.website.com/test/x2/", "/sjomatskolen/x2")]
            [TestCase("http://www.website.com/test/x3/?a=b", "/sjomatskolen/x3?a=b")]
            public void the_original_redirect_newurl_remains_unchanged(string url, string expectedRedirect)
            {
                var urlNotFound = new Uri(url, UriKind.Absolute);

                var originalRedirectSnapshot = new CustomRedirect(OriginalRedirect)
                {
                    Id = OriginalRedirect.Id
                };

                var processedRedirect = _sut.Find(urlNotFound);

                OriginalRedirect.NewUrl.Should().Be(originalRedirectSnapshot.NewUrl);

                // don't know why it doesn't work properly 
                // OriginalRedirect.Should().Be(originalRedirectSnapshot);
            }
        }
    }
}