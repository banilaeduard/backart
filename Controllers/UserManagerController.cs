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
    using WebApi.Helpers;
    using DataAccess.Entities;
    using BackArt;
    using core;

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

        [HttpPost]
        public async Task<IActionResult> createUser(UserModel userModel, [FromQuery] string resetUrl)
        {
            var identityUser = AppIdentityUserExtension.From(userModel);
            var result = await this.userManager.CreateAsync(identityUser);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(identityUser, "basic");
                await userManager.ConfirmEmailAsync(identityUser, await this.userManager.GenerateEmailConfirmationTokenAsync(identityUser));

                var token = await this.userManager.GeneratePasswordResetTokenAsync(identityUser);
                var param = new Dictionary<string, string>() {
                        { "token", token },
                        { "email", identityUser.Email }
                    };
                var resetLink = QueryHelpers.AddQueryString(resetUrl, param);
                this.emailSender.SendEmail(identityUser.Email, resetLink, "Setati parola!");
                return Ok(new UserModel().From(identityUser));
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        [HttpPatch]
        public async Task<IActionResult> updateUser(UserModel userModel)
        {
            var identityUser = await this.userManager.FindByNameAsync(userModel.UserName);
            var result = await this.userManager.UpdateAsync(identityUser.fromUserModel(userModel));
            if (result.Succeeded)
            {
                return Ok(new UserModel().From(identityUser));
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        [HttpDelete("{username}")]
        public async Task<IActionResult> DeleteUserAsync(string username)
        {
            var result = await this.userManager.DeleteAsync(await this.userManager.FindByNameAsync(username));
            return result.Succeeded ? Ok() : BadRequest(result.Errors);
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> GetUserAsync(string username)
        {
            return Ok(new UserModel().From(await this.userManager.FindByNameAsync(username)));
        }

        [HttpPost("add-to-role/{userName}")]
        public async Task<IActionResult> AddToRole(string userName, [FromQuery] RoleEnum role)
        {
            var user = await this.userManager.FindByNameAsync(userName);

            await this.userManager.RemoveFromRolesAsync(user, await this.userManager.GetRolesAsync(user));
            var result = await this.userManager.AddToRoleAsync(
                                                user,
                                                Enum.GetName(typeof(RoleEnum), role)
                                                );
            return result.Succeeded ? Ok() : BadRequest(result.Errors);
        }

        [HttpPost("remove-role/{userName}")]
        public async Task<IActionResult> RemoveRole(string userName, [FromQuery] RoleEnum role)
        {
            var result = await this.userManager.RemoveFromRoleAsync(
                                                await this.userManager.FindByNameAsync(userName),
                                                Enum.GetName(typeof(RoleEnum), role)
                                                );
            return result.Succeeded ? Ok() : BadRequest(result.Errors);
        }

        [HttpGet("roles/{userName}")]
        public async Task<IActionResult> GetUserRoles(string userName)
        {
            var roles = await this.userManager.GetRolesAsync(await this.userManager.FindByNameAsync(userName));
            return Ok(roles);
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
    }
}