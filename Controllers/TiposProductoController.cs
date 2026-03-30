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
    public class TiposProductoController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public TiposProductoController(DulcesERPContext context )
        {
            _context = context;
        }

        //OBTENER TIPOS PRODUCTOS
        [HttpGet]
        public async Task<IActionResult> GetTiposProducto()
        {
            var tiposprod = await _context.Tipos_Productos
                .OrderBy(tp => tp.tipo_producto_id)
                .ToListAsync();
            return Ok(tiposprod);
        }

        //OBTENER POR ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTiposProducto(int id)
        {
            var tiposprod = await _context.Tipos_Productos.FirstOrDefaultAsync(tp => tp.tipo_producto_id == id);

            if(tiposprod == null)
                return NotFound();

            return Ok(tiposprod);
        }

        //CREAR TIPO PRODUCTO
        [HttpPost]
        public async Task<IActionResult> CrearTiposProductos(TiposProductosDTOs dtos)
        {
            var tiposprod = new Tipos_Productos
            {
                nombre = dtos.nombre
            };

            _context.Tipos_Productos.Add(tiposprod);
            await _context.SaveChangesAsync();
            return Ok(tiposprod);   
        }

        //ACTUALIZAR TIPO PRODUCTO
        [HttpPut("{id}")]
        public async Task<IActionResult> EditarTiposProductos(int id, TiposProductosDTOs dtos)
        {
            var tiposprod = await _context.Tipos_Productos.FirstOrDefaultAsync(tp => tp.tipo_producto_id == id);

            if(tiposprod == null)
                return NotFound();

            tiposprod.nombre = dtos.nombre;
            tiposprod.activo = dtos.activo;
            _context.Tipos_Productos.Update(tiposprod);
            await _context.SaveChangesAsync();
            return Ok(tiposprod);
        }

        //ELIMINAR TIPO PRODUCTO
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarTiposProductos(int id)
        {
            var tiposprod = await _context.Tipos_Productos.FirstOrDefaultAsync(tp => tp.tipo_producto_id == id);

            if(tiposprod == null)
                return NotFound();

            _context.Tipos_Productos.Remove(tiposprod);
            await _context.SaveChangesAsync();
            return Ok("Tipo de producto eliminado correctamente");
        }
    }
}
