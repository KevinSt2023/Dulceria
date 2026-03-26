using Microsoft.AspNetCore.Mvc;
using DulcesERP.Application.DTOs;
using DulcesERP.Domain.Entities;
using DulcesERP.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Storage.Json;

namespace DulcesERP.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public UsuariosController(DulcesERPContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsuarios()
        {
            var user = await _context.Usuarios
                .Select(u => new
                {
                    u.usuario_id,
                    u.nombre,
                    u.email,
                    u.activo,
                    u.rol_id,
                    rolnombre = u.roles.nombre,                    
                })
                .OrderBy(u => u.usuario_id)
                .ToListAsync();

            return Ok(user);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetUsuarios(int id)
        {
            var user = await _context.Usuarios.FirstAsync(u => u.usuario_id == id);

            if(user == null)
                return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> CreateUsuarios(UsuariosDTOs dto)
        {
            var user = new Usuarios
            {
                nombre = dto.nombre,
                email = dto.email,
                activo = dto.activo,
                rol_id = dto.rol_id
            };

            _context.Usuarios.Add(user);
            await _context.SaveChangesAsync();           

            return Ok(user);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUsuarios(int id, UsuariosDTOs dto)
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.usuario_id == id);

            if (usuario == null)
                return NotFound();

            usuario.nombre = dto.nombre;
            usuario.email = dto.email;
            usuario.activo = dto.activo;
            usuario.rol_id = dto.rol_id;            

            await _context.SaveChangesAsync();

            return Ok(usuario);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuarios(int id)
        {
            var user = await _context.Usuarios.FirstOrDefaultAsync(u => id==u.usuario_id);

            if (user == null)
                return NotFound();

            user.activo = false;
            await _context.SaveChangesAsync();
            return Ok("Usuario borrado correctamente");
        }
    }
}
