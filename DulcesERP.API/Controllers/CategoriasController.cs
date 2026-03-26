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
    public class CategoriasController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public CategoriasController(DulcesERPContext context)
        {
            _context = context;
        }

        //LISTADO DE CATEGORIAS
        [HttpGet]
        public async Task<IActionResult> GetCategorias()
        {
            var categorias = await _context.Categorias
                .OrderBy(c => c.categoria_id)
                .ToListAsync();
            return Ok(categorias);
        }

        //BUSQUEDA POR ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategorias(int id)
        {
            var categoria = await _context.Categorias.FirstOrDefaultAsync(c => c.categoria_id == id);

            if(categoria == null)
                return NotFound();

            return Ok(categoria);
        }

        //CREAR CATEGORIA
        [HttpPost]
        public async Task<IActionResult> CrearCategoria (CategoriasDTOs dtos)
        {
            var categorias = new Categorias
            {
                nombre = dtos.nombre,
                activo = dtos.activo
            };

            _context.Categorias.Add(categorias);
            await _context.SaveChangesAsync();
            return Ok(categorias);
        }

        //ACTUALIZAR CATEGORIA
        [HttpPut("{id}")]
        public async Task<IActionResult> EditarCategoria (int id, CategoriasDTOs dtos)
        {
            var categoria = await _context.Categorias.FirstOrDefaultAsync(c => c.categoria_id == id);

            if(categoria == null)
                return NotFound();

            categoria.nombre = dtos.nombre;
            categoria.activo = dtos.activo;
            _context.Categorias.Update(categoria);
            await _context.SaveChangesAsync();

            return Ok(categoria);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarCategoria (int id)
        {
            var categoria = _context.Categorias.FirstOrDefault(c => c.categoria_id == id);

            if(categoria == null)
                return NotFound();

            categoria.activo = false;
            await _context.SaveChangesAsync();

            return Ok("Categoria desactivada correctamente");
        }

        //[HttpPut("activar/{id}")]
        //public async Task<IActionResult> ActivarCategoria(int id)
        //{
        //    var categoria = await _context.Categorias
        //        .FirstOrDefaultAsync(c => c.categoria_id == id);

        //    if (categoria == null)
        //        return NotFound();

        //    categoria.activo = true;

        //    await _context.SaveChangesAsync();

        //    return Ok(categoria);
        //}
    }
}
