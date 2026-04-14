using Microsoft.AspNetCore.Mvc;
using DulcesERP.Application.DTOs;
using DulcesERP.Domain.Entities;
using DulcesERP.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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

        // ─────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────
        private int GetRolId() => int.Parse(User.FindFirstValue("rol_id")!);
        private int GetSucursalId() => int.Parse(User.FindFirstValue("sucursal_id")!);
        private bool EsSuperAdmin() => GetRolId() == 0;
        private bool EsAdmin() => GetRolId() == 1;

        // ─────────────────────────────────────────────
        // GET /api/usuarios
        // SuperAdmin ve todos · Admin solo ve su sucursal
        // ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetUsuarios()
        {
            var query = _context.Usuarios.AsQueryable();

            // Admin de sucursal solo ve los usuarios de su sucursal
            if (!EsSuperAdmin())
                query = query.Where(u => u.sucursal_id == GetSucursalId());

            var usuarios = await query
                .Select(u => new
                {
                    u.usuario_id,
                    u.nombre,
                    u.email,
                    u.activo,
                    u.rol_id,
                    u.sucursal_id,
                    rol_nombre = u.roles.nombre,
                    sucursal_nombre = u.sucursales.nombre
                })
                .OrderBy(u => u.usuario_id)
                .ToListAsync();

            return Ok(usuarios);
        }

        // ─────────────────────────────────────────────
        // GET /api/usuarios/{id}
        // ─────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUsuario(int id)
        {
            var query = _context.Usuarios.Where(u => u.usuario_id == id);

            if (!EsSuperAdmin())
                query = query.Where(u => u.sucursal_id == GetSucursalId());

            var user = await query.FirstOrDefaultAsync();

            if (user == null)
                return NotFound("Usuario no encontrado");

            return Ok(user);
        }

        // ─────────────────────────────────────────────
        // POST /api/usuarios
        // Admin solo puede crear usuarios para su sucursal
        // y no puede asignar rol SuperAdmin ni Admin
        // ─────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> CreateUsuario(UsuariosDTOs dto)
        {
            if (!EsSuperAdmin())
            {
                // Admin solo crea usuarios en su propia sucursal
                dto.sucursal_id = GetSucursalId();

                // Admin no puede crear SuperAdmin (0) ni otro Admin (1)
                if (dto.rol_id == 0 || dto.rol_id == 1)
                    return Forbid();
            }

            // Verificar que el email no esté en uso dentro del tenant
            var emailExiste = await _context.Usuarios
                .AnyAsync(u => u.email.ToLower() == dto.email.ToLower());

            if (emailExiste)
                return BadRequest("El email ya está registrado");

            var user = new Usuarios
            {
                nombre = dto.nombre,
                email = dto.email,
                activo = dto.activo,
                rol_id = dto.rol_id,
                sucursal_id = dto.sucursal_id,
                password_hash = BCrypt.Net.BCrypt.HashPassword(dto.password_hash)
            };

            _context.Usuarios.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                user.usuario_id,
                user.nombre,
                user.email,
                user.rol_id,
                user.sucursal_id,
                user.activo
            });
        }

        // ─────────────────────────────────────────────
        // PUT /api/usuarios/{id}
        // Admin no puede mover usuarios a otra sucursal
        // ni cambiarles el rol a Admin/SuperAdmin
        // ─────────────────────────────────────────────
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUsuario(int id, UsuariosDTOs dto)
        {
            var query = _context.Usuarios.Where(u => u.usuario_id == id);

            // Admin solo puede editar usuarios de su sucursal
            if (!EsSuperAdmin())
                query = query.Where(u => u.sucursal_id == GetSucursalId());

            var usuario = await query.FirstOrDefaultAsync();

            if (usuario == null)
                return NotFound("Usuario no encontrado o sin permiso");

            if (!EsSuperAdmin())
            {
                // Admin no puede escalar roles ni mover de sucursal
                if (dto.rol_id == 0 || dto.rol_id == 1)
                    return BadRequest("No tienes permiso para asignar ese rol");

                dto.sucursal_id = GetSucursalId(); // fuerza su sucursal
            }

            usuario.nombre = dto.nombre;
            usuario.email = dto.email;
            usuario.activo = dto.activo;
            usuario.rol_id = dto.rol_id;
            usuario.sucursal_id = dto.sucursal_id;

            if (!string.IsNullOrWhiteSpace(dto.password_hash))
                usuario.password_hash = BCrypt.Net.BCrypt.HashPassword(dto.password_hash);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                usuario.usuario_id,
                usuario.nombre,
                usuario.email,
                usuario.rol_id,
                usuario.sucursal_id,
                usuario.activo
            });
        }

        // ─────────────────────────────────────────────
        // DELETE /api/usuarios/{id}
        // Soft delete — solo desactiva
        // Admin no puede eliminar usuarios de otra sucursal
        // ─────────────────────────────────────────────
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUsuario(int id)
        {
            var query = _context.Usuarios.Where(u => u.usuario_id == id);

            if (!EsSuperAdmin())
                query = query.Where(u => u.sucursal_id == GetSucursalId());

            var user = await query.FirstOrDefaultAsync();

            if (user == null)
                return NotFound("Usuario no encontrado o sin permiso");

            // Nadie puede desactivar a un SuperAdmin
            if (user.rol_id == 0)
                return BadRequest("No se puede eliminar al Super Admin");

            user.activo = false;
            await _context.SaveChangesAsync();

            return Ok("Usuario desactivado correctamente");
        }
    }
}