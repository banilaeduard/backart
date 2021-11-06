using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebApi.Services;
using System.Linq;

[ApiController]
[Route("/")]
public class HomeController : ControllerBase
{
    private IUserService userService;
    public HomeController(IUserService userService)
    {
        this.userService = userService;
    }

    [HttpGet]
    public string test()
    {
        return userService.GetAll().FirstOrDefault()?.Name;
    }
}