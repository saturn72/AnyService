
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AnyService
{
    public sealed class DefaultAuthorizationHandler : IAuthorizationHandler
    {
        private static readonly IDictionary<string, Action<AuthorizationHandlerContext>> AuthorizationWorkers
            = new Dictionary<string, Action<AuthorizationHandlerContext>>(StringComparer.CurrentCultureIgnoreCase);

        private static readonly object lockObj = new object();

        private readonly WorkContext _workContext;

        public DefaultAuthorizationHandler(WorkContext workContext)
        {
            _workContext = workContext;
        }
        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            string curHttpMethod;
            var key = GetRequestKey();
            if (!AuthorizationWorkers.TryGetValue(key, out Action<AuthorizationHandlerContext> worker))
            {
                worker = GetAuthzWorker(curHttpMethod);
                lock (lockObj)
                {
                    AuthorizationWorkers[key] = worker;
                }
            }

            worker(context);

            return Task.CompletedTask;

            string GetRequestKey()
            {
                var ad = (context.Resource as Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext).ActionDescriptor;
                var routeAtt = ad.EndpointMetadata.FirstOrDefault(x => x.GetType() == typeof(RouteAttribute)) as RouteAttribute;
                var httpMethodAtt = ad.EndpointMetadata.FirstOrDefault(x => typeof(HttpMethodAttribute).IsAssignableFrom(x.GetType())) as HttpMethodAttribute;
                curHttpMethod = httpMethodAtt.HttpMethods.First().ToLower();
                return $"{routeAtt.Name}_{curHttpMethod}";
            }
        }
        private Action<AuthorizationHandlerContext> GetAuthzWorker(string httpMethod)
        {
            var authAtt = GetAuthorizeAttribute(httpMethod);
            if (authAtt == null)
                return ctx => ctx.Fail();

            if (authAtt.Roles != null && authAtt.Roles.Any())
            {
                return ctx =>
                {
                    var res = authAtt.Roles.Any(r => ctx.User.IsInRole(r));
                    if (!res)
                        ctx.Fail();
                };
            }

            throw new System.NotImplementedException("currently only roles are supported");
        }
        private AuthorizationNode GetAuthorizeAttribute(string httpMethod)
        {
            var authz = _workContext.CurrentTypeConfigRecord.Authorization;
            switch (httpMethod)
            {
                case "post":
                    return authz.PostAuthorizeNode;
                case "get":
                    return authz.GetAuthorizeNode;
                case "put":
                    return authz.PutAuthorizeNode;
                case "delete":
                    return authz.DeleteAuthorizeNode;
            }
            return null;
        }
    }
}
