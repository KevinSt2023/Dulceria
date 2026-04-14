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

        public async Task<bool> ValidateUser(string email, string password, int rol_id)
        {
            var user = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.email == email && u.password_hash == password && u.rol_id == rol_id);
            return user != null;
        }
    }
}
