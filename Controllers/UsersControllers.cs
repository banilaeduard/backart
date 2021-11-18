namespace WebApi.Controllers
{
    using System.Threading.Tasks;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.Extensions.Logging;

    using WebApi.Services;
    using WebApi.Models;
    using WebApi.Entities;

    public class UsersController : WebApiController2
    {
        IUserService _userService;
        UserManager<AppIdentityUser> _userManager;

        public UsersController(
            IUserService userService,
            UserManager<AppIdentityUser> userManager,
            ILogger<UsersController> logger) : base(logger)
        {
            _userService = userService;
            _userManager = userManager;
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

        [HttpGet("{id}/refresh-tokens")]
        public async Task<IActionResult> GetRefreshTokens(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return Ok(user);
        }

        [HttpGet("{username}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetById(string username)
        {
            if (this.CurrentUserName != username)
                return Forbid();
            var user = await _userService.GetById((await _userManager.FindByNameAsync(username)).Id);
            if (user == null) return NotFound();

            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> updateUser(UserModel userModel)
        {
            if (userModel.Email != this.CurrentEmail) return Forbid();

            var identityUser = await this._userManager.FindByEmailAsync(userModel.Email);
            if (identityUser == null) return NotFound();

            var result = await this._userManager.UpdateAsync(userIdentityFromUserModel(identityUser, userModel));
            if (result.Succeeded)
            {
                return Ok(userModel);
            }
            else
            {
                return BadRequest(result.Errors);
            }
        }

        private AppIdentityUser userIdentityFromUserModel(AppIdentityUser user, UserModel userModel)
        {
            user.UserName = userModel.UserName;
            user.Name = userModel.Name;
            user.PhoneNumber = userModel.Phone;
            user.Address = userModel.Address;
            user.Birth = userModel.Birth;
            return user;
        }
    }
}