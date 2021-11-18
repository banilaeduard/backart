namespace WebApi.Models
{
    using System.Text.Json.Serialization;
    using WebApi.Entities;
    public class AuthenticateResponse
    {
        public string Username { get; set; }
        public string JwtToken { get; set; }

        [JsonIgnore] // refresh token is returned in http only cookie
        public string RefreshToken { get; set; }

        public AuthenticateResponse(AppIdentityUser user, string jwtToken, string refreshToken)
        {
            Username = user.UserName;
            JwtToken = jwtToken;
            RefreshToken = refreshToken;
        }
    }
}