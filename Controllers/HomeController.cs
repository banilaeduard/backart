using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace WebApi.Controllers
{
    [Route("/")]
    public class HomeController : WebApiController2
    {
        public HomeController()
        {
        }

        [HttpGet]
        [AllowAnonymous]
        public string test()
        {
            return "web api core up and running";
        }

        [HttpGet]
        [Route("ping")]
        public string ping()
        {
            return "pong";
        }
    }
}