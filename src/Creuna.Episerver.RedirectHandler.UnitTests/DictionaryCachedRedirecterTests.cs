using Creuna.Episerver.RedirectHandler.Core.CustomRedirects;
using System;
using Moq;
using NUnit.Framework;

namespace Creuna.Episerver.RedirectHandler
{
    public class DictionaryCachedRedirecterTests
    {
        private DictionaryCachedRedirecter _sut;
        private Mock<IRedirecter> _redirecterMock;

        [SetUp]
        public virtual void SetUp()
        {
            _redirecterMock = new Mock<IRedirecter>();
            _sut = new DictionaryCachedRedirecter(_redirecterMock.Object);
        }

        [Test]
        public void Redirects_are_calculated_once_per_uri()
        {
            for (var i = 0; i < 10; i++)
                _sut.Redirect(string.Empty, new Uri("http://www.website.org"));
            _redirecterMock.Verify(r => r.Redirect(string.Empty, It.IsAny<Uri>()), Times.Once);
        }

        [Test]
        public void Redirects_are_calculated_once_per_uri_and_querystring_combination()
        {
            _sut.Redirect(string.Empty, new Uri("http://www.website.org?a=1"));
            _sut.Redirect(string.Empty, new Uri("http://www.website.org?a=2"));
            _redirecterMock.Verify(r => r.Redirect(string.Empty, It.IsAny<Uri>()), Times.Exactly(2));
        }
    }
}
