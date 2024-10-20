namespace WebApi.Controllers
{
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Logging;
    using DataAccess.Entities;
    using global::WebApi.Models;
    using global::WebApi.Services;
    using AutoMapper;

    [AllowAnonymous]
    public class CreateAccountController : WebApiController2
    {
        UserManager<AppIdentityUser> userManager;
        EmailSender emailSender;

        public CreateAccountController(
            UserManager<AppIdentityUser> userManager,
            EmailSender emailSender,
            IMapper mapper,
            ILogger<CreateAccountController> logger) : base(logger, mapper)
        {
            this.userManager = userManager;
            this.emailSender = emailSender;
        }

        [HttpPost]
        public async Task<IActionResult> Create(UserModel user, [FromQuery] string confirmationUrl)
        {
            AppIdentityUser appUser = AppIdentityUserExtension.From(user);

            IdentityResult result = await userManager.CreateAsync(appUser, user.Password);
            if (result.Succeeded)
            {
                logger.LogInformation("Account creat cu succes {0}", user.Email);
                await userManager.AddToRoleAsync(appUser, "basic");
                var token = await userManager.GenerateEmailConfirmationTokenAsync(appUser);
                var result2 = await userManager.ConfirmEmailAsync(appUser, token);
                //var param = new Dictionary<string, string>() {
                //        { "token", token },
                //        { "email", user.Email }
                //    };
                //var confirmationLink = QueryHelpers.AddQueryString(confirmationUrl, param);
                //await emailSender.SendEmail(user.Email, confirmationLink, "Confirmati adresa de email");
            }
            else
            {
                logger.LogError("Account failed {0}. {1}", user.Email, result.ToString());
                return BadRequest(result.Errors);
            }

            return Ok();
        }

        [Route("confirmation-email")]
        [HttpPost]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            var identityUser = await userManager.FindByNameAsync(email);
            if (identityUser == null)
                return NotFound();

            var result = await userManager.ConfirmEmailAsync(identityUser, token);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(identityUser, "basic");
                logger.LogInformation("Account confirmat cu succes {0}", email);
                return Ok(new { user = identityUser.UserName });
            }
            else
            {
                logger.LogError("Account failed {0}. {1}", email, result.ToString());
                return BadRequest(result.Errors);
            }
        }

        [Route("reset-password")]
        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] JObject password, [FromQuery] string token, [FromQuery] string email)
        {
            var identityUser = await userManager.FindByEmailAsync(email);
            if (identityUser == null)
                return NotFound();

            var result = await userManager.ResetPasswordAsync(identityUser, token, password["password"]?.Value<string>()!);
            if (result.Succeeded)
            {
                logger.LogInformation("Parola modificata cu succes {0}", email);
                await userManager.UpdateAsync(identityUser);
                return Ok(new { user = identityUser.UserName });
            }
            else
            {
                logger.LogError("Account failed {0}. {1}", email, result.ToString());
                return BadRequest(result.Errors);
            }
        }
    }
}