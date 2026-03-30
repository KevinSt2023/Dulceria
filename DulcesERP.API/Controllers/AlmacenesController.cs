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
            var almacen = await _context.Almacenes
                .Select(a => new
                {
                    a.almacen_id,
                    a.sucursal_id,
                    a.nombre,
                    a.activo,
                    sucursalnombre = a.sucursales.nombre
                })
                .OrderBy(a => a.almacen_id)
                .ToListAsync();
            if(almacen==null)
                return NotFound();
            await _context.SaveChangesAsync();  
            return Ok(almacen);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAlmacenes(int id)
        {
            var almacen = await _context.Almacenes.FirstOrDefaultAsync(a => a.almacen_id == id);

            if (almacen == null)
                return NotFound();

            return Ok(almacen);
        }

        [HttpPost]
        public async Task<IActionResult> createAlmacenes(AlmacenesDTOs dto)
        {
            var almacen = new Almacenes
            {
                sucursal_id = dto.sucursal_id,
                nombre = dto.nombre,
                activo = dto.activo
            };

            if (almacen == null)
                return NotFound();

            _context.Almacenes.Add(almacen);    
            await _context.SaveChangesAsync();

            return Ok(almacen);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAlmacenes(int id, AlmacenesDTOs dto)
        {
            var almacen = await _context.Almacenes.FirstOrDefaultAsync(a => a.almacen_id == id);

            if(almacen == null)
                return NotFound();

            almacen.sucursal_id = dto.sucursal_id;
            almacen.nombre = dto.nombre;
            almacen.activo = dto.activo;

            await _context.SaveChangesAsync();
            return Ok(almacen);
        }        
    }
}
