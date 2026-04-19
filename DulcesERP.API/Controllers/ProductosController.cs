using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DulcesERP.Application.DTOs;
using DulcesERP.Domain.Entities;
using DulcesERP.Infrastructure.Context;
using System.Security.Claims;

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

        private int GetSucursalId() =>
            int.Parse(User.FindFirstValue("sucursal_id")!);

        // ─────────────────────────────────────────────
        // GET /api/productos
        // Panel Admin/SuperAdmin: catálogo global completo
        // Sin filtro de inventario — ve todos los productos activos
        // ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetProductos()
        {
            var sucursalId = GetSucursalId();
            var rolId = int.Parse(User.FindFirstValue("rol_id")!);

            // Admin y SuperAdmin ven el catálogo global completo
            if (rolId == 0 || rolId == 1)
            {
                var catalogo = await _context.Productos
                    .AsNoTracking()
                    .Where(p => p.activo == true)
                    .Select(p => new
                    {
                        p.producto_id,
                        p.nombre,
                        p.descripcion,
                        p.precio,
                        p.costo,
                        p.activo,
                        p.categoria_id,
                        p.unidad_id,
                        p.tipo_producto_id,
                        p.permite_pedido_sin_stock,
                        categoria = p.categorias.nombre,
                        unidades = p.unidades.nombre,
                        tipos = p.tipos.nombre,
                        // Stock total en TODAS las sucursales — solo informativo
                        stock_total = _context.Inventario
                            .Where(i => i.producto_id == p.producto_id)
                            .Sum(i => (int?)i.stock_actual) ?? 0
                    })
                    .OrderBy(p => p.nombre)
                    .ToListAsync();

                return Ok(catalogo);
            }

            // Vendedor, Producción, etc: filtrado por sucursal
            return await GetProductosPorSucursal(sucursalId);
        }

        // ─────────────────────────────────────────────
        // GET /api/productos/disponibles
        // Endpoint explícito para el modal de pedidos
        // Siempre filtra por sucursal del JWT
        // ─────────────────────────────────────────────
        [HttpGet("disponibles")]
        public async Task<IActionResult> GetProductosDisponibles()
        {
            var sucursalId = GetSucursalId();
            return await GetProductosPorSucursal(sucursalId);
        }

        // ─────────────────────────────────────────────
        // Lógica compartida de filtro por sucursal
        // ─────────────────────────────────────────────
        private async Task<IActionResult> GetProductosPorSucursal(int sucursalId)
        {
            // Configuración por sucursal — qué productos están activos y cómo
            var configSucursal = await _context.ProductoSucursales
                .AsNoTracking()
                .Where(ps => ps.sucursal_id == sucursalId && ps.activo == true)
                .Select(ps => new
                {
                    ps.producto_id,
                    ps.permite_pedido_sin_stock
                })
                .ToListAsync();

            var productoIdsActivos = configSucursal
                .Select(ps => ps.producto_id)
                .ToHashSet();

            // Diccionario producto_id → permite_pedido_sin_stock POR SUCURSAL
            var configDict = configSucursal
                .ToDictionary(ps => ps.producto_id, ps => ps.permite_pedido_sin_stock);

            // Stock de esta sucursal
            var inventarioSucursal = await _context.Inventario
                .AsNoTracking()
                .Where(i => i.almacenes.sucursal_id == sucursalId)
                .GroupBy(i => i.producto_id)
                .Select(g => new
                {
                    producto_id = g.Key,
                    stock_actual = g.Sum(i => (int)i.stock_actual),
                    stock_reservado = g.Sum(i => i.stock_reservado),
                    stock_disponible = g.Sum(i => (int)i.stock_actual) - g.Sum(i => i.stock_reservado)
                })
                .ToListAsync();

            var stockDict = inventarioSucursal
                .ToDictionary(i => i.producto_id);

            // Solo productos activos para esta sucursal
            var productos = await _context.Productos
                .AsNoTracking()
                .Where(p => p.activo == true && productoIdsActivos.Contains(p.producto_id))
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
                .OrderBy(p => p.nombre)
                .ToListAsync();

            var resultado = productos.Select(p =>
            {
                // permite_pedido_sin_stock viene de producto_sucursal (por sede)
                // no del producto global
                var permiteSinStock = configDict.TryGetValue(p.producto_id, out var v) && v;
                var inv = stockDict.TryGetValue(p.producto_id, out var i) ? i : null;

                return new
                {
                    p.producto_id,
                    p.nombre,
                    p.descripcion,
                    p.precio,
                    p.activo,
                    p.categoria_id,
                    p.unidad_id,
                    p.tipo_producto_id,
                    permite_pedido_sin_stock = permiteSinStock,
                    p.categoria,
                    p.unidades,
                    p.tipos,
                    stock_actual = inv?.stock_actual ?? 0,
                    stock_reservado = inv?.stock_reservado ?? 0,
                    stock_disponible = inv?.stock_disponible ?? 0,
                    tiene_inventario = inv != null
                };
            }).ToList();

            return Ok(resultado);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProducto(int id)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.producto_id == id);

            if (producto == null)
                return NotFound();

            return Ok(producto);
        }

        [HttpPost]
        public async Task<IActionResult> CrearProductos(ProductosDTOs dto)
        {
            if (!await _context.Categorias.AnyAsync(c => c.categoria_id == dto.categoria_id))
                return BadRequest("Categoría inválida");
            if (!await _context.Unidades_Medida.AnyAsync(c => c.unidad_id == dto.unidad_id))
                return BadRequest("Unidad de medida inválida");
            if (!await _context.Tipos_Productos.AnyAsync(c => c.tipo_producto_id == dto.tipo_producto_id))
                return BadRequest("Tipo de producto inválido");

            var producto = new Productos
            {
                unidad_id = dto.unidad_id,
                categoria_id = dto.categoria_id,
                tipo_producto_id = dto.tipo_producto_id,
                nombre = dto.nombre,
                descripcion = dto.descripcion,
                precio = dto.precio,
                costo = dto.costo,
                permite_pedido_sin_stock = dto.permite_pedido_sin_stock
            };

            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();
            return Ok(producto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditarProductos(int id, ProductosDTOs dto)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.producto_id == id);

            if (producto == null)
                return NotFound();

            if (!await _context.Categorias.AnyAsync(c => c.categoria_id == dto.categoria_id))
                return BadRequest("Categoría inválida");
            if (!await _context.Unidades_Medida.AnyAsync(c => c.unidad_id == dto.unidad_id))
                return BadRequest("Unidad de medida inválida");
            if (!await _context.Tipos_Productos.AnyAsync(c => c.tipo_producto_id == dto.tipo_producto_id))
                return BadRequest("Tipo de producto inválido");

            producto.unidad_id = dto.unidad_id;
            producto.categoria_id = dto.categoria_id;
            producto.tipo_producto_id = dto.tipo_producto_id;
            producto.nombre = dto.nombre;
            producto.descripcion = dto.descripcion;
            producto.precio = dto.precio;
            producto.costo = dto.costo;
            producto.activo = dto.activo;
            producto.permite_pedido_sin_stock = dto.permite_pedido_sin_stock;

            await _context.SaveChangesAsync();
            return Ok(producto);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.producto_id == id);

            if (producto == null)
                return NotFound();

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();
            return Ok("Producto eliminado de la lista correctamente");
        }


    }
}