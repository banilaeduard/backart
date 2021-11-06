using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

using WebApi.Entities;
using WebApi.Helpers;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CreateAccountController : ControllerBase
    {
        UserManager<IdentityUser> userManager;
        EmailSender emailSender;
        AppSettings appSettings;
        Password passHelper;

        public CreateAccountController(
        UserManager<IdentityUser> userManager,
        EmailSender emailSender,
        AppSettings appSettings,
        Password passHelper)
        {
            this.userManager = userManager;
            this.emailSender = emailSender;
            this.appSettings = appSettings;
            this.passHelper = passHelper;
        }

        [HttpPost]
        public async Task<IActionResult> Create(User user)
        {
            IdentityUser appUser = new IdentityUser
            {
                UserName = user.Name,
                Email = user.Email,
                PhoneNumber = user.Phone,
                PasswordHash = this.passHelper.GenerateSaltedHashString(user.Password)
            };

            IdentityResult result = await userManager.CreateAsync(appUser, user.Password);
            if (result.Succeeded)
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(appUser);
                var confirmationLink = Url.Action("ConfirmEmail", "CreateAccount", new { token, email = user.Email }, Request.Scheme);
                this.emailSender.SendEmail(user.Email, confirmationLink);
            }
            else
            {
                // log errors
            }

            return Ok();
        }
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