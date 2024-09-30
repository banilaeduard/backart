namespace WebApi.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Logging;

    using DataAccess.Entities;
    using global::WebApi.Services;
    using global::WebApi.Models;
    using System.Security.Claims;
    using Microsoft.Extensions.Caching.Memory;
    using Pegasus.Common;

    public class UsersController : WebApiController2
    {
        IUserService _userService;
        UserManager<AppIdentityUser> _userManager;
        EmailSender _emailService;
        private static MemoryCache _userCache = new(new MemoryCacheOptions() { ExpirationScanFrequency = TimeSpan.FromDays(2) } );

        public UsersController(
            IUserService userService,
            UserManager<AppIdentityUser> userManager,
            ILogger<UsersController> logger,
            EmailSender emailService
            ) : base(logger)
        {
            _userService = userService;
            _userManager = userManager;
            _emailService = emailService;
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequest model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                this.logger.LogWarning("Userul nu exista {0}", model.Username);
                return NotFound(new { message = "Userul nu exista" });
            }

            bool confirmedEmail = await _userManager.IsEmailConfirmedAsync(user);

            if (!confirmedEmail)
            {
                this.logger.LogWarning("Trebuie sa confirmati emailul pentru {0}", model.Username);
                return BadRequest(new { message = "Confirmati userul accesand link-ul trimis pe email" });
            }

            bool isLockedOut = await _userManager.IsLockedOutAsync(user);
            if (isLockedOut)
            {
                this.logger.LogWarning("Contul este blocat, prea multe incercari de a introduce o parola gresita probabil pentru {0}", model.Username);
                return BadRequest(new { message = "Contul este blocat, prea multe incercari de a introduce o parola gresita probabil" });
            }

            var response = await _userService.Authenticate(model, this.ipAddress());

            if (response == null)
            {
                this.logger.LogWarning("Userul {0} nu s-a putut autentifica", model.Username);
                return BadRequest(new { message = "Username sau parola incorecte" });
            }

            this.setTokenCookie(response.RefreshToken);

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            this.logger.LogInformation("Refresh Token: {0}", refreshToken);

            var response = await _userService.RefreshToken(refreshToken, this.ipAddress());

            if (response == null)
                return Forbid();

            this.setTokenCookie(response.RefreshToken);

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("reset-password/{username}")]
        public async Task<IActionResult> ResetPassword(string username, [FromQuery] string passwordResetUrl)
        {
            var user = await _userManager.FindByNameAsync(username);

            if (user == null)
                return Forbid();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var param = new Dictionary<string, string>() {
                        { "token", token },
                        { "email", user.Email }
                    };
            var passwordResetLink = QueryHelpers.AddQueryString(passwordResetUrl, param);
            //_emailService.SendEmail(user.Email, passwordResetLink, "Resetati parola");

            return Ok();
        }

        [HttpPost("reset-password-form/{username}")]
        public async Task<IActionResult> ResetPasswordForm(string username)
        {
            if (this.CurrentUserName != username)
                return Forbid();

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return Forbid();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            return Ok(new { token });
        }

        [AllowAnonymous]
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken()
        {
            // accept token from request body or cookie
            var token = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return BadRequest(new { message = "Token is required" });

            var response = await _userService.RevokeToken(token, base.ipAddress());

            if (!response)
                return NotFound(new { message = "Token not found" });

            Response.Cookies.Delete("refreshToken");
            return Ok(new { message = "Token revoked" });
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> GetById(string username)
        {
            if (CurrentUserName != username)
                return Forbid();

            if (!_userCache.TryGetValue(username, out UserModel user))
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetPriority(CacheItemPriority.NeverRemove);

                user = await _userService.GetById((await _userManager.FindByNameAsync(username))!.Id);
                if (user == null) return NotFound();
                _userCache.Set(username, user, cacheEntryOptions);
            }
            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> updateUser(UserModel userModel)
        {
            if (userModel.Email != this.CurrentEmail) return Forbid();

            var identityUser = await this._userManager.FindByEmailAsync(userModel.Email);
            if (identityUser == null) return NotFound();

/*            var tempKey = identityUser.DataKey;
            identityUser.fromUserModel(userModel).DataKey = tempKey;*/
            var result = await this._userManager.UpdateAsync(identityUser);
            if (result.Succeeded)
            {
                return Ok(userModel);
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        [HttpPost("isInRoles")]
        public async Task<IActionResult> isInRoles(IList<string> roles)
        {
            var role = HttpContext.User.FindFirst(ClaimTypes.Role);

            return Ok(roles.Contains(role.Value));
        }
    }
}