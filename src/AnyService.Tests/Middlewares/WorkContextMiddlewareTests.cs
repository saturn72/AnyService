using System.Security.Claims;
using System.Threading.Tasks;
using AnyService.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AnyService.Tests.Middlewares
{
    public class WorkContextMiddlewareTests
    {
        [Fact]
        public async Task UserIdIsNull_ReturnUnauthroized()
        {
            var logger = new Mock<ILogger<WorkContextMiddleware>>();
            var wcm = new WorkContextMiddleware(null, logger.Object);

            var user = new Mock<ClaimsPrincipal>();
            user.Setup(u => u.Claims).Returns(new Claim[] { });
            var hc = new Mock<HttpContext>();
            var hr = new Mock<HttpResponse>();
            hc.Setup(h => h.User).Returns(user.Object);
            hc.Setup(h => h.Response).Returns(hr.Object);
            await wcm.InvokeAsync(hc.Object, null);
            hr.VerifySet(r => r.StatusCode = StatusCodes.Status401Unauthorized, Times.Once);
        }

    }
}