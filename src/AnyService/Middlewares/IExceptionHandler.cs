using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AnyService.Middlewares
{
    public interface IExceptionHandler
    {
        Task Handle(HttpContext context, object payload);
    }
}