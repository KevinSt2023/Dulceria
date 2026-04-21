// DulcesERP.API.Controllers/ProductoSucursalController.cs

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
    public class ProductoSucursalController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public ProductoSucursalController(DulcesERPContext context)
        {
            _context = context;
        }

        private int GetRolId() => int.Parse(User.FindFirstValue("rol_id")!);
        private int GetSucursalId() => int.Parse(User.FindFirstValue("sucursal_id")!);


        [HttpGet]
        public async Task<IActionResult> GetConfig()
        {
            var sucursalId = GetSucursalId();

            var productos = await _context.Productos
                .AsNoTracking()
                .Where(p => p.activo == true)
                .ToListAsync();

            var configSede = await _context.ProductoSucursales
                .AsNoTracking()
                .Where(ps => ps.sucursal_id == sucursalId)
                .ToListAsync();

            // ← Traer inventario por separado (no dentro del Select)
            var inventarioSede = await _context.Inventario
                .AsNoTracking()
                .Include(i => i.almacenes)
                .Where(i => i.almacenes.sucursal_id == sucursalId)
                .GroupBy(i => i.producto_id)
                .Select(g => new
                {
                    producto_id = g.Key,
                    stock_actual = g.Sum(i => (int)i.stock_actual)
                })
                .ToListAsync();

            var resultado = productos.Select(p =>
            {
                var config = configSede
                    .FirstOrDefault(ps => ps.producto_id == p.producto_id);

                var inv = inventarioSede
                    .FirstOrDefault(i => i.producto_id == p.producto_id);

                return new
                {
                    p.producto_id,
                    p.nombre,
                    p.precio,
                    stock_actual = inv?.stock_actual ?? 0,
                    activo_en_sucursal = config?.activo ?? false,
                    // ← Fallback correcto al valor global
                    permite_pedido_sin_stock = config != null
                        ? config.permite_pedido_sin_stock
                        : p.permite_pedido_sin_stock
                };
            }).ToList();

            return Ok(resultado);
        }

        // ─────────────────────────────────────────────
        // PUT /api/productosucursal/{productoId}
        // Admin configura un producto para su sucursal
        // ─────────────────────────────────────────────
        [HttpPut("{productoId}")]
        public async Task<IActionResult> UpdateConfig(int productoId, [FromBody] ConfigProductoSucursalDTO dto)
        {
            var rolId = GetRolId();
            var sucursalId = GetSucursalId();

            if (rolId != 0 && rolId != 1)
                return Forbid();

            var config = await _context.ProductoSucursales
                .FirstOrDefaultAsync(ps =>
                    ps.producto_id == productoId &&
                    ps.sucursal_id == sucursalId);

            if (config == null)
            {
                // Crear configuración si no existe
                config = new ProductoSucursal
                {
                    producto_id = productoId,
                    sucursal_id = sucursalId,
                    activo = dto.activo,
                    permite_pedido_sin_stock = dto.permite_pedido_sin_stock
                };
                _context.ProductoSucursales.Add(config);
            }
            else
            {
                config.activo = dto.activo;
                config.permite_pedido_sin_stock = dto.permite_pedido_sin_stock;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Configuración actualizada",
                config.activo,
                config.permite_pedido_sin_stock
            });
        }
    }
        
    public class ConfigProductoSucursalDTO
    {
        public bool activo { get; set; }
        public bool permite_pedido_sin_stock { get; set; }
    }
}