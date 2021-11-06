using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Services;

namespace WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class CreateAccountController : ControllerBase
    {
        UserManager<IdentityUser> userManager;
        EmailSender emailSender;
        AppSettings appSettings;
        IUserService userService;
        DataContext userCtx;

        public CreateAccountController(
            UserManager<IdentityUser> userManager,
            EmailSender emailSender,
            AppSettings appSettings,
            IUserService userService,
            DataContext userCtx)
        {
            this.userManager = userManager;
            this.emailSender = emailSender;
            this.appSettings = appSettings;
            this.userService = userService;
            this.userCtx = userCtx;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Create(User user)
        {
            IdentityUser appUser = new IdentityUser
            {
                UserName = user.Email,
                Email = user.Email,
                PhoneNumber = user.Phone
            };

            IdentityResult result = await userManager.CreateAsync(appUser, user.Password);
            if (result.Succeeded)
            {
                user.Password = appUser.PasswordHash;
                this.userCtx.Users.Add(user);
                this.userCtx.SaveChanges();

                var token = await userManager.GenerateEmailConfirmationTokenAsync(appUser);
                var confirmationLink = Url.Action("ConfirmEmail", "CreateAccount", new { token, email = user.Email }, Request.Scheme);
                this.emailSender.SendEmail(user.Email, confirmationLink);
            }
            else
            {
                return BadRequest(result.Errors);
            }

            return Ok();
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
                return NotFound();

            var result = await userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded ? Ok() : BadRequest();
        }
    }
}