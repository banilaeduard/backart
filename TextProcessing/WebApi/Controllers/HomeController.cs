namespace WebApi.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Logging;
    using AutoMapper;

    [Route("/")]
    public class HomeController : WebApiController2
    {
        public HomeController(
            ILogger<HomeController> logger, IMapper mapper) : base(logger, mapper)
        {
        }

        [HttpGet]
        [AllowAnonymous]
        public string test()
        {
            logger.LogInformation("web api core up and running");
            return "web api core up and running";
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        [Route("ping")]
        public IActionResult ping()
        {
            return Ok("pong");
        }
    }
}