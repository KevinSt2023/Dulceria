using DulcesERP.Application.DTOs;
using DulcesERP.Domain.Entities;
using DulcesERP.Infrastructure.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DulcesERP.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AlmacenesController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public AlmacenesController(DulcesERPContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAlmacenes()
        {
            var rolId = int.Parse(User.FindFirstValue("rol_id")!);
            var sucursalId = int.Parse(User.FindFirstValue("sucursal_id")!);

            var query = _context.Almacenes
                .Where(a => a.activo == true)
                .AsQueryable();

            // Admin solo ve almacenes de su sucursal
            if (rolId != 0)
            {
                query = query.Where(a => a.sucursal_id == sucursalId);
            }

            var almacenes = await query
                .Select(a => new
                {
                    a.almacen_id,
                    a.sucursal_id,
                    a.nombre,
                    a.activo,
                    sucursalnombre = a.sucursales.nombre
                })
                .OrderBy(a => a.sucursal_id)
                .ThenBy(a => a.nombre)
                .ToListAsync();

            return Ok(almacenes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAlmacen(int id)
        {
            var almacen = await _context.Almacenes
                .Where(a => a.almacen_id == id)
                .Select(a => new
                {
                    a.almacen_id,
                    a.sucursal_id,
                    a.nombre,
                    a.activo,
                    sucursalnombre = a.sucursales.nombre
                })
                .FirstOrDefaultAsync();

            if (almacen == null)
                return NotFound();

            return Ok(almacen);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAlmacen(AlmacenesDTOs dto)
        {
            var sucursalExiste = await _context.Sucursales
                .AnyAsync(s => s.sucursal_id == dto.sucursal_id);

            if (!sucursalExiste)
                return BadRequest("La sucursal no existe");

            var almacen = new Almacenes
            {
                sucursal_id = dto.sucursal_id,
                nombre = dto.nombre,
                activo = dto.activo
            };

            _context.Almacenes.Add(almacen);
            await _context.SaveChangesAsync();
            return Ok(almacen);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAlmacen(int id, AlmacenesDTOs dto)
        {
            var almacen = await _context.Almacenes
                .FirstOrDefaultAsync(a => a.almacen_id == id);

            if (almacen == null)
                return NotFound();

            almacen.sucursal_id = dto.sucursal_id;
            almacen.nombre = dto.nombre;
            almacen.activo = dto.activo;

            await _context.SaveChangesAsync();
            return Ok(almacen);
        }
    }
}