using Microsoft.IdentityModel.Tokens;
using RestaurantApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RestaurantApi.Helpers
{
    public static class JWTGenerator
    {
        public static string GenerateToken(User user, string jwtKey)
        {
            var claims = new[]
            {
                new Claim("nameid", user.UserID.ToString()),
                new Claim("name", user.UserName),
                new Claim("role", user.RoleId.ToString()),
                new Claim("branch", user.BranchID.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
