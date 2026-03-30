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
    public class ProductosController : Controller
    {        
        private readonly DulcesERPContext _context;

        public ProductosController(DulcesERPContext context)
        {
            _context = context;
        }    
        
        [HttpGet]
        public async Task<IActionResult> GetProductos()
        {
            var productos = await _context.Productos
                .Select(p => new
                {
                    p.producto_id,                    
                    p.nombre,
                    p.descripcion,
                    p.precio,
                    p.activo,
                    p.categoria_id,
                    p.unidad_id,
                    p.tipo_producto_id,
                    categoria = p.categorias.nombre,
                    unidades = p.unidades.nombre,
                    tipos = p.tipos.nombre
                })
                .OrderBy(p => p.producto_id)
                .ToListAsync();

            return Ok(productos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductos(int id)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.producto_id == id);
            if(producto == null)
                return NotFound();
            return Ok(producto);
        }

        [HttpPost]
        public async Task<IActionResult> CrearProductos(ProductosDTOs dto)
        {
            var producto = new Productos
            {
                unidad_id = dto.unidad_id,
                categoria_id = dto.categoria_id,
                tipo_producto_id = dto.tipo_producto_id,
                nombre = dto.nombre,
                descripcion = dto.descripcion,
                precio = dto.precio,
                costo = dto.costo
            };

            _context.Productos.Add(producto);
            if (!await _context.Categorias.AnyAsync(c => c.categoria_id == dto.categoria_id))
                return BadRequest("Categoría inválida");
            if (!await _context.Unidades_Medida.AnyAsync(c => c.unidad_id == dto.unidad_id))
                return BadRequest("Unidad de medida inválida");
            if (!await _context.Tipos_Productos.AnyAsync(c => c.tipo_producto_id == dto.tipo_producto_id))
                return BadRequest("Tipo de producto inválido");
            await _context.SaveChangesAsync();            
            return Ok(producto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditarProductos(int id, ProductosDTOs dtos)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.producto_id == id);

            if (producto == null)
                return NotFound();

            producto.unidad_id = dtos.unidad_id;
            producto.categoria_id = dtos.categoria_id;
            producto.tipo_producto_id = dtos.tipo_producto_id;
            producto.nombre = dtos.nombre;
            producto.descripcion = dtos.descripcion;
            producto.precio = dtos.precio;
            producto.costo = dtos.costo;
            producto.activo = dtos.activo;

            _context.Productos.Update(producto);
            if (!await _context.Categorias.AnyAsync(c => c.categoria_id == dtos.categoria_id))
                return BadRequest("Categoría inválida");
            if (!await _context.Unidades_Medida.AnyAsync(c => c.unidad_id == dtos.unidad_id))
                return BadRequest("Unidad de medida inválida");
            if (!await _context.Tipos_Productos.AnyAsync(c => c.tipo_producto_id == dtos.tipo_producto_id))
                return BadRequest("Tipo de producto inválido");
            await _context.SaveChangesAsync();
            return Ok(producto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            var producto = await _context.Productos.FirstOrDefaultAsync(p => p.producto_id == id);

            if (producto == null)
                return NotFound();

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();
            return Ok("Producto eliminado de la lista correctamente");
        }
    }
}
