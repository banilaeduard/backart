namespace WebApi.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.WebUtilities;

    using WebApi.Models;
    using WebApi.Entities;
    using WebApi.Helpers;

    [Authorize(Roles = "admin")]
    public class UserManagerController : WebApiController2
    {
        private UserManager<AppIdentityUser> userManager;
        private EmailSender emailSender;
        public UserManagerController(
            ILogger<UserManagerController> logger,
            UserManager<AppIdentityUser> userManager,
            EmailSender emailSender) : base(logger)
        {
            this.userManager = userManager;
            this.emailSender = emailSender;
        }

        [HttpPost("create")]
        public async Task<IActionResult> createUser(UserModel userModel, [FromQuery] string resetUrl)
        {
            var identityUser = AppIdentityUser.From(userModel);
            var result = await this.userManager.CreateAsync(identityUser,
                                                      new Guid().ToString() + DateTime.Now.ToLongTimeString());
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(identityUser, "basic");
                var token = await this.userManager.GeneratePasswordResetTokenAsync(identityUser);
                var param = new Dictionary<string, string>() {
                        { "token", token },
                        { "email", identityUser.Email }
                    };
                var resetLink = QueryHelpers.AddQueryString(resetUrl, param);
                this.emailSender.SendEmail(identityUser.Email, resetLink, "Setati parola!");
                return Ok();
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        [HttpPost("add-to-role")]
        public async Task<IActionResult> AddToRole(string userName, RoleEnum role)
        {
            var result = await this.userManager.AddToRoleAsync(
                                                await this.userManager.FindByNameAsync(userName),
                                                Enum.GetName(typeof(RoleEnum), role)
                                                );
            return result.Succeeded ? Ok() : BadRequest(result.Errors);
        }

        [HttpPost("remove-role")]
        public async Task<IActionResult> RemoveRole(string userName, RoleEnum role)
        {
            var result = await this.userManager.RemoveFromRoleAsync(
                                                await this.userManager.FindByNameAsync(userName),
                                                Enum.GetName(typeof(RoleEnum), role)
                                                );
            return result.Succeeded ? Ok() : BadRequest(result.Errors);
        }

        [HttpGet("users/{page}/{take}")]
        public IActionResult GetUsersAsync(int page, int take)
        {
            return Ok(new
            {
                users = this.userManager.Users
                            .OrderBy(user => user.Email)
                            .Skip((page - 1) * take)
                            .Take(take)
                            .Select(usr => new UserModel().From(usr))
                            .ToList(),
                count = this.userManager.Users.Count()
            });
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> GetUserAsync(string username)
        {
            return Ok(new UserModel().From(await this.userManager.FindByNameAsync(username)));
        }

        [HttpDelete("{username}")]
        public async Task<IActionResult> DeleteUserAsync(string username)
        {
            var result = await this.userManager.DeleteAsync(await this.userManager.FindByNameAsync(username));
            return result.Succeeded ? Ok() : BadRequest(result.Errors);
        }
    }
}