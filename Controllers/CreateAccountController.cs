namespace WebApi.Controllers
{
    using System.Threading.Tasks;
    using System.Collections.Generic;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.Logging;

    using WebApi.Entities;
    using WebApi.Helpers;
    using WebApi.Models;

    public class CreateAccountController : WebApiController2
    {
        UserManager<AppIdentityUser> userManager;
        EmailSender emailSender;
        AppSettings appSettings;

        public CreateAccountController(
            UserManager<AppIdentityUser> userManager,
            EmailSender emailSender,
            AppSettings appSettings,
            ILogger<CreateAccountController> logger) : base(logger)
        {
            this.userManager = userManager;
            this.emailSender = emailSender;
            this.appSettings = appSettings;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Create(UserModel user, [FromQuery] string confirmationUrl)
        {
            AppIdentityUser appUser = new AppIdentityUser
            {
                UserName = user.UserName ?? user.Email,
                Email = user.Email,
                PhoneNumber = user.Phone,
            };

            IdentityResult result = await userManager.CreateAsync(appUser, user.Password);
            if (result.Succeeded)
            {
                this.logger.LogInformation("Account creat cu succes {0}", user.Email);

                var token = await userManager.GenerateEmailConfirmationTokenAsync(appUser);
                var param = new Dictionary<string, string>() {
                        { "token", token },
                        { "email", user.Email }
                    };
                var confirmationLink = QueryHelpers.AddQueryString(confirmationUrl, param);
                this.emailSender.SendEmail(user.Email, confirmationLink);
            }
            else
            {
                this.logger.LogError("Account failed {0}. {1}", user.Email, result.ToString());
                return BadRequest(result.Errors);
            }

            return Ok();
        }

        [AllowAnonymous]
        [Route("confirmation-email")]
        [HttpPost]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            var identityUser = await userManager.FindByEmailAsync(email);
            if (identityUser == null)
                return NotFound();

            var result = await userManager.ConfirmEmailAsync(identityUser, token);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(identityUser, "basic");
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }
    }
}