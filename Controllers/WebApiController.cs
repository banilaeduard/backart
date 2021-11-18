namespace WebApi.Controllers
{
    using System;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Logging;

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class WebApiController2 : ControllerBase
    {
        protected ILogger logger;

        protected string CurrentUserName
        {
            get { return (User.FindFirst(ClaimTypes.GivenName)?.Value!); }
        }

        protected string CurrentEmail
        {
            get { return (User.FindFirst(ClaimTypes.Email)?.Value!); }
        }

        protected string CurrentUserId
        {
            get { return (User.FindFirst(ClaimTypes.Name)?.Value!); }
        }

        public WebApiController2(ILogger logger)
        {
            this.logger = logger;
        }
        protected string ipAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            else
                return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }

        protected void setTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                IsEssential = true
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }
    }
}
