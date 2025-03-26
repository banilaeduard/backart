namespace WebApi.Controllers
{
    using System;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Logging;
    using AutoMapper;
    using System.Text.RegularExpressions;

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class WebApiController2 : ControllerBase
    {
        protected const string wordType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        protected const string octetStream = "application/octet-stream";

        protected ILogger logger;
        protected IMapper mapper;

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

        public WebApiController2(ILogger logger, IMapper mapper)
        {
            this.logger = logger;
            this.mapper = mapper;
        }

        protected string ipAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"]!;
            else
                return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString()!;
        }

        protected void setTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                IsEssential = true,
                Secure = true,
                SameSite = SameSiteMode.None
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        protected async Task WriteStreamToResponse(Stream stream, string fName, string contentType)
        {
            try
            {
                if (stream.CanSeek && stream.Position > 0)
                    stream.Position = 0;
                Response.Headers["Content-Disposition"] = $@"attachment; filename={fName}";
                Response.ContentType = contentType;
                await stream.CopyToAsync(Response.BodyWriter.AsStream());
            }
            finally
            {
                stream.Close();
            }
        }

        protected string SanitizeFileName(string name)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            string pattern = $"[{Regex.Escape(invalidChars)}]";
            return Regex.Replace(name, pattern, "");
        }

    }
}
