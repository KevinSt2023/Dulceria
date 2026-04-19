using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DulcesERP.Application.Services
{
    public class JwtServices
    {
        private readonly IConfiguration _configuration;

        public JwtServices(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(int userId, int tenantId, int sucursalId, string sucursalNombre,int rolId, string rolNombre, string email)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
            );

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("usuario_id",  userId.ToString()),
                new Claim("tenant_id",   tenantId.ToString()),
                new Claim("sucursal_id", sucursalId.ToString()),
                new Claim("sucursal_nombre", sucursalNombre),
                new Claim("rol_id",      rolId.ToString()),
                new Claim("rol_nombre",  rolNombre),
                new Claim(ClaimTypes.Email, email)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                                        Convert.ToDouble(_configuration["Jwt:ExpireMinutes"])
                                    ),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

