using System;
using Creuna.Episerver.RedirectHandler.Core.Configuration;
using Creuna.Episerver.RedirectHandler.Core.CustomRedirects;
using Creuna.Episerver.RedirectHandler.Core.Logging;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Should;

namespace Creuna.Episerver.RedirectHandler
{
    [TestFixture]
    public class RedirecterTests
    {
        private readonly static Fixture Fixture = new Fixture();
        private Redirecter _sut;
        private RedirectConfiguration _configuration;
        private CustomRedirectCollection _redirects;

        [SetUp]
        public virtual void SetUp()
        {
            _configuration = new RedirectConfiguration();
            _redirects = new CustomRedirectCollection();
            _sut = new Redirecter(_redirects, _configuration);
            RequestLogger.Instance = new RequestLogger(_configuration);
        }

        public class When_a_redirect_is_set_up_with_append : RedirecterTests
        {
            private CustomRedirect _redirect;

            public override void SetUp()
            {
                base.SetUp();
                _redirect = new CustomRedirect("/no/", "/new/?redirected=1", true, false, true);
                _redirects.Add(_redirect);
            }

            public class _and_query_strings_are_not_included : When_a_redirect_is_set_up_with_append
            {
                public override void SetUp()
                {
                    base.SetUp();
                    _redirect.IncludeQueryString = false;
                }

                [Test]
                public void _then_only_the_query_string_of_the_redirect_is_appended_to_the_new_url()
                {
                    _sut.Redirect(string.Empty, new Uri("http://www.website.com/no/look/to/norway?test=xyz", 
                        UriKind.Absolute))
                        .NewUrl.ShouldEqual("/new/look/to/norway?redirected=1");
                }
            }

            public class _and_query_strings_are_included : When_a_redirect_is_set_up_with_append
            {
                public override void SetUp()
                {
                    base.SetUp();
                    _redirect.IncludeQueryString = true;
                }

                [Test]
                public void _then_the_query_string_is_forwarded()
                {
                    _sut.Redirect(string.Empty, new Uri("http://www.website.com/no/look/to/norway?test=xyz", UriKind.Absolute))
                        .NewUrl.ShouldEqual("/new/look/to/norway?redirected=1&test=xyz");
                }
            }
        }

        public class When_a_custom_redirect_is_set_up_with_exact_match : RedirecterTests
        {
            public override void SetUp()
            {
                base.SetUp();

                _redirects.Add(new CustomRedirect("/no", "newurl", false, true, false, 0));
            }

            [Test]
            [TestCase("http://www.somewhere.com/nooooooo")]
            [TestCase("http://www.somewhere.com/no/")]
            public void Partial_match_should_not_get_redirected(string oldUri)
            {
                var result = _sut.Redirect("", new Uri(oldUri));
                result.Redirected.ShouldBeFalse();
            }

            [Test]
            public void Exact_match_should_be_redirected()
            {
                _sut.Redirect("", new Uri("http://www.somewhere.com/no"))
                    .NewUrl.ShouldEqual("newurl");
            }

            [Test]
            public void Querystrings_are_included_in_redirected_if_specified_to_do_so()
            {
                var newUri = "new/uri";
                var incomingQuerystring = string.Format("{0}={1}&{2}={3}",
                    Fixture.Create<string>(),
                    Fixture.Create<string>(),
                    Fixture.Create<string>(),
                    Fixture.Create<string>());
                _redirects.Add(new CustomRedirect("/old-uri",
                    newUri, false, true, true));
                _sut.Redirect("", new Uri("http://www.incoming.com/old-uri?" + incomingQuerystring))
                    .NewUrl.ShouldEqual(newUri + "?" + incomingQuerystring);
            }

            [Test]
            public void Querystring_are_appended_to_existing_querystring()
            {
                const string newUri = "new/uri?a=1";
                const string incomingQuerystring = "b=2";
                _redirects.Add(new CustomRedirect("/old-uri",
                    newUri, false, true, true));
                _sut.Redirect("", new Uri("http://www.incoming.com/old-uri?" + incomingQuerystring))
                    .NewUrl.ShouldEqual(newUri + "&" + incomingQuerystring);
            }

            [Test]
            [TestCase("new/uri?a=1")]
            [TestCase("new/uri")]
            public void Missing_querystring_are_handled(string newUri)
            {
                const string incomingQuerystring = "";
                _redirects.Add(new CustomRedirect("/old-uri",
                    newUri, false, true, true));
                _sut.Redirect("", new Uri("http://www.incoming.com/old-uri?" + incomingQuerystring))
                    .NewUrl.ShouldEqual(newUri);
            }

            [Test]
            public void Chained_redirects_are_collapsed()
            {
                const string oldUri = "/a";
                const string newUri = "/d";
                _redirects.Add(new CustomRedirect(oldUri,

                    "/b", false, true, true));
                _redirects.Add(new CustomRedirect("/b",
                    "/c", false, true, true));
                _redirects.Add(new CustomRedirect("/c",
                    newUri, false, true, true));
                _sut.Redirect("", new Uri("http://www.incoming.com/" + oldUri.Substring(1)))
                    .NewUrl.ShouldEqual(newUri);
            }

            [Test]
            [ExpectedException(typeof(RedirectLoopException))]
            public void Redirect_loops_throws_exceptions()
            {
                _redirects.Add(new CustomRedirect("/a",
                    "/b", false, true, true));
                _redirects.Add(new CustomRedirect("/b",
                    "/a", false, true, true));
                _sut.Redirect("", new Uri("http://www.incoming.com/" + "a"));
            }

            [Test]
            public void Redirects_set_up_without_querystrings_match_incoming_uri_with_querystrings()
            {
                _redirects.Add(new CustomRedirect("/a",
                    "/b", false, true, true));
                _sut.Redirect("", new Uri("http://www.incoming.com/" + "a?x=y"))
                    .NewUrl.ShouldEqual("/b?x=y");
            }
        }

        public class When_a_redirect_query_string_and_append_configured : RedirecterTests
        {
            [Test]
            public void _with_not_exact_match_No_duplication_should_occur()
            {
                _redirects.Add(new CustomRedirect("/no", "/newurl",
                    exactMatch: false,
                    appendMatchToNewUrl: true,
                    includeQueryString: true));

                _sut.Redirect("", new Uri("http://www.somewhere.com/no/path?param=value"))
                    .NewUrl.ShouldEqual("/newurl/path?param=value");
            }

            [Test]
            public void _with_exact_match_No_duplication_should_occur()
            {
                _redirects.Add(new CustomRedirect("/no", "/newurl",
                    exactMatch: true,
                    appendMatchToNewUrl: true,
                    includeQueryString: true));

                _sut.Redirect("", new Uri("http://www.somewhere.com/no?param=value"))
                    .NewUrl.ShouldEqual("/newurl?param=value");
            }
        }

        public class When_an_old_url_is_set_up_both_with_and_without_querystrings : RedirecterTests
        {
            [Test]
            public void _then_the_most_specific_redirect_is_used()
            {
                _redirects.Add(new CustomRedirect("/a",
                    "/b", false, true, true));
                _redirects.Add(new CustomRedirect("/a?x=y",
                    "/correct", false, true, true));
                _sut.Redirect("", new Uri("http://www.incoming.com/" + "a?x=y"))
                    .NewUrl.ShouldEqual("/correct?x=y");
            }

            [Test]
            public void _then_the_querystring_is_only_forwarded_if_specified()
            {
                _redirects.Add(new CustomRedirect("/a",
                    "/b", false, true, true));
                _redirects.Add(new CustomRedirect("/a?x=y",
                    "/correct", false, true, false));
                _sut.Redirect("", new Uri("http://www.incoming.com/" + "a?x=y"))
                    .NewUrl.ShouldEqual("/correct");
            }
        }

        public class When_the_target_url_is_absolute : RedirecterTests
        {
            [Test]
            public void Redirects_without_querystrings_can_be_targeted_at_absolute_uris()
            {
                _redirects.Add(new CustomRedirect("/a",
                    "http://www.externalsite.com", false, true, false));
                _sut.Redirect(string.Empty, new Uri("http://mysite.com/a"))
                    .NewUrl.ShouldEqual("http://www.externalsite.com");
            }

            [Test]
            public void Redirects_with_querystrings_can_be_targeted_at_absolute_uris()
            {
                _redirects.Add(new CustomRedirect("/a",
                    "http://www.externalsite.com", false, true, false));
                _sut.Redirect(string.Empty, new Uri("http://mysite.com/a?test=1"))
                    .NewUrl.ShouldEqual("http://www.externalsite.com");
            }

            [Test]
            public void Redirects_with_querystrings_can_be_targeted_at_absolute_uris_with_forwarded_querystrings()
            {
                _redirects.Add(new CustomRedirect("/a",
                    "http://www.externalsite.com", false, true, true));
                _sut.Redirect(string.Empty, new Uri("http://mysite.com/a?test=1"))
                    .NewUrl.ShouldEqual("http://www.externalsite.com?test=1");
            }

            [Test]
            public void
                Redirects_with_querystrings_can_be_targeted_at_absolute_uris_with_forwarded_querystrings_and_append()
            {
                _redirects.Add(new CustomRedirect("/a",
                    "http://www.externalsite.com", true, false, true));
                _sut.Redirect(string.Empty, new Uri("http://mysite.com/a/b"))
                    .NewUrl.ShouldEqual("http://www.externalsite.com/b");
            }

            [Test]
            public void Absolute_uris_with_nonexact_match_does_not_throw_loop_exception()
            {
                _redirects.Add(new CustomRedirect("/a",
                    "http://www.externalsite.com", true, false, true));
                _sut.Redirect(string.Empty, new Uri("http://mysite.com/a/aaaaaa"))
                    .NewUrl.ShouldEqual("http://www.externalsite.com/aaaaaa");
            }

            [Test]
            public void Append_with_querystrings_included_redirects_to_appended_url_with_querystring()
            {
                _redirects.Add(new CustomRedirect("/de/",
                    "/de-welcome/", true, true, true));
                _sut.Redirect(string.Empty, new Uri("http://mysite.com/de/?parameter=value"))
                    .NewUrl.ShouldEqual("/de-welcome/?parameter=value");
            }
        }
    }
}
