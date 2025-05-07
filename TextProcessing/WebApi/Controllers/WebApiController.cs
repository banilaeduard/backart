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
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
    using Microsoft.ServiceFabric.Services.Client;
    using V2.Interfaces;
    using Microsoft.ServiceFabric.Services.Remoting;
    using MetadataService.Interfaces;

    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class WebApiController2 : ControllerBase
    {
        protected Lazy<ServiceProxyFactory> serviceProxy = new Lazy<ServiceProxyFactory>(() => new ServiceProxyFactory((c) =>
        {
            return new FabricTransportServiceRemotingClientFactory();
        }));

        protected Dictionary<string, Uri> SFURL = new Dictionary<string, Uri>()
        {
            { nameof(IWorkLoadService), new Uri("fabric:/TextProcessing/WorkLoadService") },
            { nameof(IMetadataServiceFabric), new Uri("fabric:/TextProcessing/MetadataService") }
        };

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

        protected async Task WriteStreamToResponse(Stream stream, string fName, string contentType, bool resetStream = true)
        {
            try
            {
                if (stream.CanSeek && stream.Position > 0 && resetStream)
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

        

        protected T GetService<T>() where T : IService
        {
            return serviceProxy.Value.CreateServiceProxy<T>(SFURL[typeof(T).Name], ServicePartitionKey.Singleton);
        }

        protected string SanitizeFileName(string name)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            string pattern = $"[{Regex.Escape(invalidChars)}]";
            return Regex.Replace(name, pattern, "");
        }

    }
}
