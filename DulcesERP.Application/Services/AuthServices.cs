using BCrypt.Net;
using DulcesERP.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace DulcesERP.Application.Services
{
    public class AuthServices
    {
        private readonly DulcesERPContext _context;

        public AuthServices(DulcesERPContext context)
        {
            _context = context;
        }

        // Validación correcta con BCrypt — nunca comparar password en texto plano
        public async Task<bool> ValidateUser(string email, string password)
        {
            var user = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.email.Trim().ToLower() == email.Trim().ToLower() &&
                    u.activo);

            if (user == null) return false;

            return BCrypt.Net.BCrypt.Verify(password, user.password_hash);
        }
    }
}