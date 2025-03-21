using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using WebApi.Models;
using DataAccess.Entities;


namespace WebApi.Services
{
    public interface IUserService
    {
        Task<AuthenticateResponse> Authenticate(AuthenticateRequest model, string ipAddress);
        Task<AuthenticateResponse> RefreshToken(string token, string ipAddress);
        Task<bool> RevokeToken(string token, string ipAddress);
        IEnumerable<UserModel> GetAll();
        Task<UserModel> GetById(string id);
    }

    public class UserService : IUserService
    {
        // private DataContext _context;
        private UserManager<AppIdentityUser> _userManager;
        public UserService(
            UserManager<AppIdentityUser> userManager)
        {
            // _context = context;
            _userManager = userManager;
        }

        public async Task<AuthenticateResponse> Authenticate(AuthenticateRequest model, string ipAddress)
        {
            var identityUser = await _userManager.FindByNameAsync(model.Username);
            if (identityUser == null) return null;

            var passwordCheck = _userManager.PasswordHasher.VerifyHashedPassword(identityUser, identityUser.PasswordHash, model.Password);
            if (passwordCheck == PasswordVerificationResult.Failed)
            {
                //await _userManager.AccessFailedAsync(identityUser);
                return null;
            }
            // authentication successful so generate jwt and refresh tokens
            var jwtToken = await generateJwtToken(identityUser);
            var refreshToken = generateRefreshToken(ipAddress);

            // save refresh token
            await _userManager.UpdateAsync(identityUser);

            return new AuthenticateResponse(identityUser, jwtToken, refreshToken);
        }

        public async Task<AuthenticateResponse> RefreshToken(string token, string ipAddress)
        {
            AppIdentityUser user = null;//_userManager.Users.SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));

            // return null if no user found with token
            if (user == null) return null;

            //var refreshToken = user.RefreshTokens.Single(x => x.Token == token);

            // return null if token is no longer active
            //if (!refreshToken.IsActive) return null;

            // replace old refresh token with a new one and save
            var newRefreshToken = generateRefreshToken(ipAddress);
            //refreshToken.Revoked = DateTime.UtcNow;
            //refreshToken.RevokedByIp = ipAddress;
            //refreshToken.ReplacedByToken = newRefreshToken.Token;
            //user.RefreshTokens.Add(newRefreshToken);
            await _userManager.UpdateAsync(user);
            // generate new jwt
            var jwtToken = await generateJwtToken(user);

            return new AuthenticateResponse(user, jwtToken, token);
        }

        public async Task<bool> RevokeToken(string token, string ipAddress)
        {
            AppIdentityUser user = null;//_userManager.Users.SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));

            // return false if no user found with token
            if (user == null) return false;

            //var refreshToken = user.RefreshTokens.Single(x => x.Token == token);

            // return false if token is not active
            //if (!refreshToken.IsActive) return false;

            //// revoke token and save
            //refreshToken.Revoked = DateTime.UtcNow;
            //refreshToken.RevokedByIp = ipAddress;
            //await _userManager.UpdateAsync(user);

            return true;
        }

        public IEnumerable<UserModel> GetAll()
        {
            var users = new List<UserModel>();
            foreach (var user in _userManager.Users)
            {
                users.Add(new UserModel()
                {
                    UserName = user.UserName,
                    Name = user.Name,
                    Id = user.Id,
                    Address = user.Address,
                    Email = user.Email,
                    Password = "",
                    Phone = user.PhoneNumber,
                    //DataKey = user.DataKeyLocation.locationCode,
                });
            }
            return users;
        }

        public async Task<UserModel> GetById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            return new UserModel()
            {
                UserName = user.UserName,
                Name = user.Name,
                Id = user.Id,
                Address = user.Address,
                Email = user.Email,
                Password = "",
                Phone = user.PhoneNumber,
                //DataKey = user.DataKeyLocation.locationCode,
            };
        }
        // helper methods

        private async Task<string> generateJwtToken(AppIdentityUser appUser)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("Secret")!);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, appUser.Id?.ToString() ?? ""),
                    new Claim(ClaimTypes.Email, appUser.Email?.ToString()?? ""),
                    new Claim(ClaimTypes.StreetAddress, appUser.Address?.ToString() ?? ""),
                    new Claim(ClaimTypes.GivenName, appUser.UserName?.ToString()??""),
                    new Claim(ClaimTypes.MobilePhone, appUser.PhoneNumber?.ToString()??""),
                    new Claim(ClaimTypes.Role, string.Join(",", await _userManager.GetRolesAsync(appUser))?? ""),
                    new Claim(ClaimTypes.Actor, appUser.Tenant??"cubik"),
                    //new Claim("dataKeyLocation", appUser.DataKeyLocation?.locationCode ??""),
                    //new Claim("dataKeyId", appUser.DataKeyLocation?.Id.ToString() ??""),
                    //new Claim("dataKeyName", appUser.DataKeyLocation?.name ??"")
                }),
                Expires = DateTime.UtcNow.AddHours(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string generateRefreshToken(string ipAddress)
        {
            using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                var randomBytes = new byte[64];
                rngCryptoServiceProvider.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes);
            }
        }
    }
}