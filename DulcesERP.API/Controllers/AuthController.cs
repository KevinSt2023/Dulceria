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
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context.Usuarios
                .IgnoreQueryFilters()
                .Include(u => u.roles)
                .Include(u => u.sucursales)
                .FirstOrDefaultAsync(u =>
                    u.email.Trim().ToLower() == request.email.Trim().ToLower());

            if (user == null)
                return Unauthorized("Usuario no existe");

            if (!BCrypt.Net.BCrypt.Verify(request.password, user.password_hash))
                return Unauthorized("Password incorrecto");

            // Nombre de sucursal — SuperAdmin (rol 0) ve todas
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