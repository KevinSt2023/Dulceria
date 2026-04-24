using DulcesERP.Application.DTOs;
using DulcesERP.Application.Services;
using DulcesERP.Infrastructure.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DulcesERP.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly DulcesERPContext _context;
        private readonly JwtServices _jwtServices;

        public AuthController(DulcesERPContext context, JwtServices jwtServices)
        {
            _context = context;
            _jwtServices = jwtServices;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Mensaje genérico siempre — nunca revelar si el email existe o no
            const string mensajeError = "Credenciales incorrectas";

            if (string.IsNullOrWhiteSpace(request.email) ||
                string.IsNullOrWhiteSpace(request.password))
                return Unauthorized(mensajeError);

            var user = await _context.Usuarios
                .IgnoreQueryFilters()
                .Include(u => u.roles)
                .Include(u => u.sucursales)
                .FirstOrDefaultAsync(u =>
                    u.email.Trim().ToLower() == request.email.Trim().ToLower());

            // Verificar siempre BCrypt aunque no exista el usuario
            // para evitar timing attacks
            var hashDummy = "$2a$11$dummyhashparaevitartimingattacksxxxxxxxxxxxxxxxxxxx";
            var passwordValido = user != null &&
                BCrypt.Net.BCrypt.Verify(request.password, user.password_hash);

            if (user == null)
            {
                BCrypt.Net.BCrypt.Verify(request.password, hashDummy);
                return Unauthorized(mensajeError);
            }

            if (!passwordValido)
                return Unauthorized(mensajeError);

            if (!user.activo)
                return Unauthorized(mensajeError);

            var sucursalNombre = user.rol_id == 0
                ? "Todas las sucursales"
                : user.sucursales?.nombre ?? $"Sucursal #{user.sucursal_id}";

            var rolNombre = user.roles?.nombre ?? "Sin rol";

            var token = _jwtServices.GenerateToken(
                userId: user.usuario_id,
                tenantId: user.tenant_id,
                sucursalId: user.sucursal_id,
                sucursalNombre: sucursalNombre,
                rolId: user.rol_id,
                rolNombre: rolNombre,
                email: user.email
            );

            return Ok(new
            {
                token,
                sucursal_nombre = sucursalNombre,
                rol_nombre = rolNombre,
                rol_id = user.rol_id
            });
        }
    }
}