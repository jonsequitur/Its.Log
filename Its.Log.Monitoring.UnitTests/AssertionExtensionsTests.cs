using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace Its.Log.Monitoring.UnitTests
{
    [TestFixture]
    public class AssertionExtensionsTests
    {
        [Test]
        public async Task When_ShouldSucceed_is_passed_a_failed_response_it_throws()
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);

            Action assert = () => response.ShouldSucceed();

            assert.ShouldThrow<AssertionFailedException>();
        }

        [Test]
        public async Task When_ShouldFailWith_is_passed_a_successful_response_it_throws()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            Action assert = () => response.ShouldFailWith(HttpStatusCode.Forbidden);

            assert.ShouldThrow<AssertionFailedException>();
        }

        [Test]
        public async Task When_ShouldSucceed_is_passed_a_successful_response_it_doesnt_throw()
        {
            var response = new HttpResponseMessage(HttpStatusCode.Accepted);

            Action assert = () => response.ShouldSucceed();

            assert.ShouldNotThrow();
        }

        [Test]
        public async Task When_ShouldFailWith_is_passed_a_failed_response_it_doesnt_throw()
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);

            Action assert = () => response.ShouldFailWith(HttpStatusCode.BadRequest);

            assert.ShouldNotThrow();
        }

        [Test]
        public async Task When_ShouldSucceedAsync_is_passed_a_failed_response_it_throws()
        {
            var response = Task.Run(() => new HttpResponseMessage(HttpStatusCode.BadRequest));

            Action assert = () =>
            {
                var x = response.ShouldSucceedAsync().Result;
            };

            assert.ShouldThrow<AssertionFailedException>();
        }

        [Test]
        public async Task When_ShouldFailWithAsync_is_passed_a_successful_response_it_throws()
        {
            var response = Task.Run(() => new HttpResponseMessage(HttpStatusCode.OK));

            Action assert = () =>
            {
                var x = response.ShouldFailWithAsync(HttpStatusCode.Forbidden).Result;
            };

            assert.ShouldThrow<AssertionFailedException>();
        }

        [Test]
        public async Task When_ShouldSucceedAsync_is_passed_a_successful_response_it_doesnt_throw()
        {
            var response = Task.Run(() => new HttpResponseMessage(HttpStatusCode.Accepted));

            Action assert = () =>
            {
                var x = response.ShouldSucceedAsync().Result;
            };

            assert.ShouldNotThrow();
        }

        [Test]
        public async Task When_ShouldFailWithAsync_is_passed_a_failed_response_it_doesnt_throw()
        {
            var response = Task.Run(() => new HttpResponseMessage(HttpStatusCode.BadRequest));

            Action assert = () =>
            {
                var x = response.ShouldFailWithAsync(HttpStatusCode.BadRequest).Result;
            };

            assert.ShouldNotThrow();
        }
    }
}