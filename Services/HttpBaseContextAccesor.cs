using DataAccess;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BackArt.Services
{
    public class HttpBaseContextAccesor : IBaseContextAccesor
    {
        IHttpContextAccessor _baseContext;
        public HttpBaseContextAccesor(IHttpContextAccessor baseContext)
        {
            _baseContext = baseContext;
        }
        public string TenantId => _baseContext.HttpContext.User.FindFirst(ClaimTypes.Actor).Value;

        public string DataKey => _baseContext.HttpContext.User.FindFirst("dataKey").Value;

        public bool IsAdmin => _baseContext.HttpContext.User.FindFirst(ClaimTypes.Role).Value == "admin";

        public bool disableFiltering => false;
    }
}
