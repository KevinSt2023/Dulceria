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
    public class UnidadesMedidaController : Controller
    {
        private readonly DulcesERPContext _context;

        public UnidadesMedidaController(DulcesERPContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetUnidadesMedida()
        {
            var unidadesMedida = await _context.Unidades_Medida
                .OrderBy(u => u.unidad_id)
                .ToListAsync();
            return Ok(unidadesMedida);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUnidadesMedida(int id)
        {
            var unidadMedida = await _context.Unidades_Medida.FirstOrDefaultAsync(u => u.unidad_id == id);
            if (unidadMedida == null)
                return NotFound();
            return Ok(unidadMedida);
        }

        [HttpPost]
        public async Task<IActionResult> CrearUnidadMedida(Unidades_MedidaDTOs dtos)
        {
            var unidadMedida = new Unidades_Medida
            {
                nombre = dtos.nombre,
                abreviatura = dtos.abreviatura
            };
            _context.Unidades_Medida.Add(unidadMedida);
            await _context.SaveChangesAsync();
            return Ok(unidadMedida);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditarUnidadMedida(int id, Unidades_MedidaDTOs dtos)
        {
            var unidadMedida = await _context.Unidades_Medida.FirstOrDefaultAsync(u => u.unidad_id == id);
            if (unidadMedida == null)
                return NotFound();
            unidadMedida.nombre = dtos.nombre;
            unidadMedida.abreviatura = dtos.abreviatura;
            unidadMedida.activo = dtos.activo;
            _context.Unidades_Medida.Update(unidadMedida);
            await _context.SaveChangesAsync();
            return Ok(unidadMedida);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarUnidadMedida(int id)
        {
            var unidadMedida = await _context.Unidades_Medida.FirstOrDefaultAsync(u => u.unidad_id == id);
            if (unidadMedida == null)
                return NotFound();
            _context.Unidades_Medida.Remove(unidadMedida);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
