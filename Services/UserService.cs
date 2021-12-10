using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using WebApi.Models;
using WebApi.Entities;
using WebApi.Helpers;

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
        private readonly AppSettings _appSettings;
        private UserManager<AppIdentityUser> _userManager;
        public UserService(
            IOptions<AppSettings> appSettings,
            UserManager<AppIdentityUser> userManager)
        {
            // _context = context;
            _appSettings = appSettings.Value;
            _userManager = userManager;
        }

        public async Task<AuthenticateResponse> Authenticate(AuthenticateRequest model, string ipAddress)
        {
            var identityUser = await _userManager.FindByNameAsync(model.Username);
            if (identityUser == null) return null;

            var passwordCheck = _userManager.PasswordHasher.VerifyHashedPassword(identityUser, identityUser.PasswordHash, model.Password);
            if (passwordCheck == PasswordVerificationResult.Failed)
            {
                await _userManager.AccessFailedAsync(identityUser);
                return null;
            }
            // authentication successful so generate jwt and refresh tokens
            var jwtToken = await generateJwtToken(identityUser);
            var refreshToken = generateRefreshToken(ipAddress);

            // save refresh token
            identityUser.RefreshTokens.Add(refreshToken);
            await _userManager.UpdateAsync(identityUser);

            return new AuthenticateResponse(identityUser, jwtToken, refreshToken.Token);
        }

        public async Task<AuthenticateResponse> RefreshToken(string token, string ipAddress)
        {
            var user = _userManager.Users.SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));

            // return null if no user found with token
            if (user == null) return null;

            var refreshToken = user.RefreshTokens.Single(x => x.Token == token);

            // return null if token is no longer active
            if (!refreshToken.IsActive) return null;

            // replace old refresh token with a new one and save
            var newRefreshToken = generateRefreshToken(ipAddress);
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.ReplacedByToken = newRefreshToken.Token;
            user.RefreshTokens.Add(newRefreshToken);
            await _userManager.UpdateAsync(user);
            // generate new jwt
            var jwtToken = await generateJwtToken(user);

            return new AuthenticateResponse(user, jwtToken, newRefreshToken.Token);
        }

        public async Task<bool> RevokeToken(string token, string ipAddress)
        {
            var user = _userManager.Users.SingleOrDefault(u => u.RefreshTokens.Any(t => t.Token == token));

            // return false if no user found with token
            if (user == null) return false;

            var refreshToken = user.RefreshTokens.Single(x => x.Token == token);

            // return false if token is not active
            if (!refreshToken.IsActive) return false;

            // revoke token and save
            refreshToken.Revoked = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            await _userManager.UpdateAsync(user);

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
                    Birth = user.Birth,
                    Email = user.Email,
                    Password = "",
                    Phone = user.PhoneNumber
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
                Birth = user.Birth,
                Email = user.Email,
                Password = "",
                Phone = user.PhoneNumber
            };
        }

        // helper methods

        private async Task<string> generateJwtToken(AppIdentityUser appUser)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
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

                }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private RefreshToken generateRefreshToken(string ipAddress)
        {
            using (var rngCryptoServiceProvider = new RNGCryptoServiceProvider())
            {
                var randomBytes = new byte[64];
                rngCryptoServiceProvider.GetBytes(randomBytes);
                return new RefreshToken
                {
                    Token = Convert.ToBase64String(randomBytes),
                    Expires = DateTime.UtcNow.AddDays(7),
                    Created = DateTime.UtcNow,
                    CreatedByIp = ipAddress
                };
            }
        }
    }
}