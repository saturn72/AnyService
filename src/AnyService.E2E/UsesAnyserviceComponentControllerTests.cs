﻿using AnyService.SampleApp.Identity;
using Shouldly;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace AnyService.E2E
{
    public class UsesAnyserviceComponentControllerTests : E2ETestBase
    {
        public UsesAnyserviceComponentControllerTests(E2EFixture fixture, ITestOutputHelper outputHelper) :
            base(fixture, outputHelper)
        {
        }

        [Fact(Skip ="feature not done yet:")]
        public async Task Get()
        {
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(ManagedAuthenticationHandler.AuthorizedJson1);

            var res = await HttpClient.GetStringAsync("api/standalone");
            res.ShouldNotBeNull();
        }
    }
}
