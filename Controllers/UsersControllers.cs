using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

using WebApi.Services;
using WebApi.Models;
using WebApi.Helpers;
using WebApi.Entities;

namespace WebApi.Controllers
{
    public class UsersController : WebApiController2
    {
        IUserService _userService;
        UserManager<AppIdentityUser> _userManager;
        DataContext _userDB;

        public UsersController(
            IUserService userService,
            UserManager<AppIdentityUser> userManager,
            DataContext context)
        {
            _userService = userService;
            _userManager = userManager;
            _userDB = context;
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequest model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                return NotFound(new { message = "Userul nu exista" });
            }

            bool confirmedEmail = await _userManager.IsEmailConfirmedAsync(user);

            if (!confirmedEmail)
            {
                return BadRequest(new { message = "Confirmati userul accesand link-ul trimis pe email" });
            }

            bool isLockedOut = await _userManager.IsLockedOutAsync(user);
            if (isLockedOut)
            {
                return BadRequest(new { message = "Contul este blocat, prea multe incercari de a introduce o parola gresita probabil" });
            }

            var response = await _userService.Authenticate(model, user, this.ipAddress());

            if (response == null)
                return BadRequest(new { message = "Username sau parola incorecte" });

            this.setTokenCookie(response.RefreshToken);

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public IActionResult RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var response = _userService.RefreshToken(refreshToken, this.ipAddress());

            if (response == null)
                return Forbid();

            this.setTokenCookie(response.RefreshToken);

            return Ok(response);
        }

        [AllowAnonymous]
        [HttpPost("revoke-token")]
        public IActionResult RevokeToken()
        {
            var user = User;
            // accept token from request body or cookie
            var token = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return BadRequest(new { message = "Token is required" });

            var response = _userService.RevokeToken(token, base.ipAddress());

            if (!response)
                return NotFound(new { message = "Token not found" });

            Response.Cookies.Delete("refreshToken");
            return Ok(new { message = "Token revoked" });
        }

        [HttpGet("{id}/refresh-tokens")]
        public IActionResult GetRefreshTokens(string id)
        {
            var user = _userService.GetById(id);
            if (user == null) return NotFound();

            return Ok(user.RefreshTokens);
        }

        [HttpGet("{username}")]
        public async Task<IActionResult> GetById(string username)
        {
            var user = _userService.GetById((await _userManager.FindByEmailAsync(username)).Id);
            if (user == null) return NotFound();

            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> updateUser(UserModel userModel)
        {
            if (userModel.Email != this.getContextUser().Email) return Forbid();

            var identityUser = await this._userManager.FindByEmailAsync(userModel.Email);
            if (identityUser == null) return NotFound();

            var user = _userService.GetById(identityUser.Id);
            if (user == null) return NotFound();

            var result = await this._userManager.UpdateAsync(identityUser.mapFromUser(userModel));
            if (result.Succeeded)
            {
                _userDB.Update(user.From(userModel));
                _userDB.SaveChanges();
            }
            else
            {
                return BadRequest(result.Errors);
            }
            return Ok(user);
        }
    }
}