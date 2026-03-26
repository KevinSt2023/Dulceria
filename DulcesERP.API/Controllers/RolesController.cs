using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DulcesERP.Application.DTOs;
using DulcesERP.Domain.Entities;
using DulcesERP.Infrastructure.Context;

namespace DulcesERP.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public RolesController(DulcesERPContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _context.Roles.OrderBy(r => r.rol_id).ToListAsync();
            return Ok(roles);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoles(int id)
        {
            var rol = await _context.Roles.FirstAsync(r => r.rol_id == id);
            if (rol == null)
                return NotFound();
            return Ok(rol);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoles(RolesDTOs dtos)
        {
            var rol = new Roles
            {
                nombre = dtos.nombre,
                activo = dtos.activo
            };
            _context.Roles.Add(rol);
            await _context.SaveChangesAsync();
            return Ok(rol);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRoles(int id, RolesDTOs dtos)
        {
            var rol = await _context.Roles.FirstOrDefaultAsync(r => r.rol_id == id);
            if (rol == null)
                return NotFound();
            rol.nombre = dtos.nombre;
            rol.activo = dtos.activo;
            _context.Roles.Update(rol);
            await _context.SaveChangesAsync();
            return Ok(rol);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoles(int id)
        {
            var rol = await _context.Roles.FirstOrDefaultAsync(r => r.rol_id == id);
            if (rol == null)
                return NotFound();
            _context.Roles.Remove(rol);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
