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
    public class SucursalesController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public SucursalesController(DulcesERPContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetSucursales()
        {
            var rolId = int.Parse(User.FindFirstValue("rol_id")!);
            var sucursalId = int.Parse(User.FindFirstValue("sucursal_id")!);

            var query = _context.Sucursales.AsQueryable();

            // Admin solo ve su sucursal en el panel de configuración
            // SuperAdmin ve todas
            // Para el selector de pickup en pedidos, el frontend llama con ?todas=true
            var todasParam = Request.Query.ContainsKey("todas");

            if (rolId != 0 && !todasParam)
            {
                query = query.Where(s => s.sucursal_id == sucursalId);
            }

            var sucursales = await query
                .Where(s => s.activo == true)
                .Select(s => new
                {
                    s.sucursal_id,
                    s.nombre,
                    s.direccion,
                    s.telefono,
                    s.activo
                })
                .OrderBy(s => s.sucursal_id)
                .ToListAsync();

            return Ok(sucursales);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSucursales(int id)
        {
            var sucursal = await _context.Sucursales.FirstOrDefaultAsync(s => s.sucursal_id == id);
            if (sucursal == null)
                return NotFound();
            return Ok(sucursal);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSucursales(SucursalesDTOs dto)
        {
            var sucursal = new Sucursales
            {
                nombre = dto.nombre,
                direccion = dto.direccion,
                telefono = dto.telefono,
                activo = true
            };
            _context.Sucursales.Add(sucursal);
            await _context.SaveChangesAsync();
            return Ok(sucursal);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSucursales(int id, SucursalesDTOs dto)
        {
            var sucursal = await _context.Sucursales.FirstOrDefaultAsync(s => s.sucursal_id == id);
            if (sucursal == null)
                return NotFound();
            sucursal.nombre = dto.nombre;
            sucursal.direccion = dto.direccion;
            sucursal.telefono = dto.telefono;
            sucursal.activo = dto.activo;
            await _context.SaveChangesAsync();
            return Ok(sucursal);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSucursales(int id)
        {
            var sucursal = await _context.Sucursales.FirstOrDefaultAsync(s => s.sucursal_id == id);
            if (sucursal == null)
                return NotFound();
            _context.Sucursales.Remove(sucursal);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
