using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("/")]
public class HomeController : ControllerBase
{
    public HomeController()
    {
    }

    [HttpGet]
    public string test()
    {
        return "web api core up and running";
    }
}