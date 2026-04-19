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

        // ─────────────────────────────────────────────
        // GET /api/productosucursal
        // Admin ve la configuración de su sucursal
        // SuperAdmin puede ver cualquier sucursal con ?sucursal_id=X
        // ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetConfig()
        {
            var rolId = GetRolId();
            var sucursalId = GetSucursalId();

            // SuperAdmin puede consultar otra sucursal
            if (rolId == 0 && Request.Query.TryGetValue("sucursal_id", out var qSucursal))
                sucursalId = int.Parse(qSucursal!);

            // Solo Admin y SuperAdmin
            if (rolId != 0 && rolId != 1)
                return Forbid();

            // Todos los productos activos del catálogo global
            var productos = await _context.Productos
                .AsNoTracking()
                .Where(p => p.activo == true)
                .OrderBy(p => p.nombre)
                .Select(p => new { p.producto_id, p.nombre, p.precio })
                .ToListAsync();

            // Configuración actual de esta sucursal
            var config = await _context.ProductoSucursales
                .AsNoTracking()
                .Where(ps => ps.sucursal_id == sucursalId)
                .ToListAsync();

            var configDict = config.ToDictionary(ps => ps.producto_id);

            // Stock de esta sucursal
            var stock = await _context.Inventario
                .AsNoTracking()
                .Where(i => i.almacenes.sucursal_id == sucursalId)
                .GroupBy(i => i.producto_id)
                .Select(g => new
                {
                    producto_id = g.Key,
                    stock_actual = g.Sum(i => (int)i.stock_actual)
                })
                .ToListAsync();

            var stockDict = stock.ToDictionary(s => s.producto_id, s => s.stock_actual);

            var resultado = productos.Select(p =>
            {
                var cfg = configDict.TryGetValue(p.producto_id, out var c) ? c : null;
                return new
                {
                    p.producto_id,
                    p.nombre,
                    p.precio,
                    activo_en_sucursal = cfg?.activo ?? false,
                    permite_pedido_sin_stock = cfg?.permite_pedido_sin_stock ?? true,
                    stock_actual = stockDict.TryGetValue(p.producto_id, out var s) ? s : 0,
                    configurado = cfg != null
                };
            });

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