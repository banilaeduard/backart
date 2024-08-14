using DataAccess;
using System.Security.Claims;

namespace WebApi.Services
{
    public class HttpBaseContextAccesor : IBaseContextAccesor
    {
        IHttpContextAccessor _baseContext;
        public HttpBaseContextAccesor(IHttpContextAccessor baseContext)
        {
            _baseContext = baseContext;
        }
        public string TenantId => _baseContext.HttpContext.User.FindFirst(ClaimTypes.Actor).Value;

        public bool IsAdmin => _baseContext.HttpContext.User.FindFirst(ClaimTypes.Role).Value == "admin";

        public bool disableFiltering => false;

        public string DataKeyLocation => _baseContext.HttpContext.User.FindFirst("dataKeyLocation").Value;

        public string DataKeyName => _baseContext.HttpContext.User.FindFirst("dataKeyName").Value;

        public string DataKeyId => _baseContext.HttpContext.User.FindFirst("dataKeyId").Value;
    }
}
