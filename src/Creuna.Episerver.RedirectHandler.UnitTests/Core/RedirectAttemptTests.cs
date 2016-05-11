using NUnit.Framework;
using Ploeh.AutoFixture;
using Should;

namespace Creuna.Episerver.RedirectHandler.Core
{
    public class RedirectAttemptTests
    {
        private readonly Fixture Fixture = new Fixture();

        [Test]
        public void Two_RedirectAttempts_to_same_url_are_reference_equal()
        {
            var url = Fixture.Create<string>();
            (RedirectAttempt.Success(url) == RedirectAttempt.Success(url)).ShouldBeTrue();
        }

        [Test]
        public void Two_RedirectAttempts_to_different_urls_are_not_equal()
        {
            var url = Fixture.Create<string>();
            (RedirectAttempt.Success(url) == RedirectAttempt.Success(Fixture.Create<string>())).ShouldBeFalse();
        }

        [Test]
        public void Two_RedirectAttempts_to_same_url_are_value_equal()
        {
            var url = Fixture.Create<string>();
            (RedirectAttempt.Success(url).Equals(RedirectAttempt.Success(url))).ShouldBeTrue();
        }
    }
}
