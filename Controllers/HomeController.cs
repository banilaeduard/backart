using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("/")]
public class HomeController : ControllerBase
{
    [HttpGet]
    public string test()
    {
        return "Hello World!";
    }
}