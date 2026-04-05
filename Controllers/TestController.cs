using DulcesERP.Domain.Entities;
using DulcesERP.Infrastructure.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DulcesERP.API.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public TestController(DulcesERPContext context)
        {
            _context = context;
        }

        [HttpGet("usuarios")]
        public async Task<IActionResult> GetUsuarios() 
        {
            var usuarios = await _context.Usuarios.ToListAsync();
            return Ok(usuarios);
        }

        [Authorize]
        [HttpGet("tenant")]
        public IActionResult GetTenant()
        {
            var tenantId = User.FindFirst("tenant_id")?.Value;
            var userId = User.FindFirst("user_id")?.Value;

            return Ok(new
            {
                tenant = tenantId,
                usuario = userId
            });
        }

        [Authorize]
        [HttpPost("crear-usuario-test")]
        public async Task<IActionResult> CrearUsuarioTest()
        {
            var usuario = new Usuarios
            {
                nombre = "Usuario Test",
                email = "test@email.com",
                password_hash = "1234",
                activo = true,
                created_at = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return Ok(usuario);
        }
        //994361619 -- MTQBM-00012868-2026
    }
}
