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
    public class InventarioController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public InventarioController(DulcesERPContext context)
        {
            _context = context;
        }

        private int GetRolId() => int.Parse(User.FindFirstValue("rol_id")!);
        private int GetSucursalId() => int.Parse(User.FindFirstValue("sucursal_id")!);

        // ─────────────────────────────────────────────
        // GET /api/inventario
        // ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetInventario()
        {
            var rolId = GetRolId();
            var sucursalId = GetSucursalId();

            var query = _context.Inventario.AsQueryable();

            if (rolId != 0)
                query = query.Where(i => i.almacenes.sucursal_id == sucursalId);

            var data = await query
                .Select(i => new
                {
                    i.inventario_id,
                    i.producto_id,
                    i.almacen_id,
                    stock_actual = (int)i.stock_actual,
                    i.stock_reservado,
                    stock_disponible = (int)i.stock_actual - i.stock_reservado,
                    i.stock_minimo,
                    i.stock_maximo,
                    nombreproducto = i.productos.nombre,
                    almacennombre = i.almacenes.nombre,
                    sucursal_id = i.almacenes.sucursal_id,
                    sucursalnombre = i.almacenes.sucursales.nombre,
                    estado =
                        i.stock_actual <= i.stock_minimo ? "BAJO" :
                        i.stock_actual >= i.stock_maximo ? "ALTO" :
                        "NORMAL"
                })
                .OrderBy(i => i.sucursal_id)
                .ThenBy(i => i.nombreproducto)
                .ToListAsync();

            return Ok(data);
        }

        // ─────────────────────────────────────────────
        // POST /api/inventario — Registrar movimiento
        // ─────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> RegistrarMovimiento(MovimientoInventarioDTOs dto)
        {
            var rolId = GetRolId();
            var sucursalId = GetSucursalId();

            if (dto.cantidad <= 0)
                return BadRequest("Cantidad inválida");

            if (string.IsNullOrWhiteSpace(dto.motivo))
                return BadRequest("Debe ingresar un motivo");

            if (rolId != 0)
            {
                var almacenValido = await _context.Almacenes
                    .AnyAsync(a => a.almacen_id == dto.almacen_id
                                && a.sucursal_id == sucursalId);
                if (!almacenValido)
                    return Forbid();
            }

            var productoExiste = await _context.Productos
                .AnyAsync(p => p.producto_id == dto.producto_id);
            if (!productoExiste)
                return BadRequest("Producto no existe");

            var almacenExiste = await _context.Almacenes
                .AnyAsync(a => a.almacen_id == dto.almacen_id);
            if (!almacenExiste)
                return BadRequest("Almacén no existe");

            var tiposValidos = new[] { "ENTRADA", "SALIDA", "AJUSTE" };
            if (!tiposValidos.Contains(dto.tipo_movimiento))
                return BadRequest("Tipo de movimiento inválido");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var inventario = await _context.Inventario
                    .FirstOrDefaultAsync(i =>
                        i.producto_id == dto.producto_id &&
                        i.almacen_id == dto.almacen_id);

                if (inventario == null)
                {
                    inventario = new Inventario
                    {
                        producto_id = dto.producto_id,
                        almacen_id = dto.almacen_id,
                        stock_actual = 0,
                        stock_reservado = 0,
                        stock_minimo = 0,
                        stock_maximo = 0,
                        updated_at = DateTime.UtcNow
                    };
                    _context.Inventario.Add(inventario);
                    await _context.SaveChangesAsync(); // necesario para tener el id
                }

                int stockAntes = (int)inventario.stock_actual;
                int stockDespues = stockAntes;

                if (dto.tipo_movimiento == "SALIDA" &&
                    inventario.stock_actual < dto.cantidad)
                    return BadRequest("Stock insuficiente");

                switch (dto.tipo_movimiento)
                {
                    case "ENTRADA": stockDespues += (int)dto.cantidad; break;
                    case "SALIDA": stockDespues -= (int)dto.cantidad; break;
                    case "AJUSTE": stockDespues = (int)dto.cantidad; break;
                }

                if (stockDespues < 0)
                    return BadRequest("Stock no puede ser negativo");

                inventario.stock_actual = stockDespues;
                inventario.updated_at = DateTime.UtcNow;

                _context.InventarioMovimientos.Add(new InventarioMovimiento
                {
                    producto_id = dto.producto_id,
                    almacen_id = dto.almacen_id,
                    tipo_movimiento = dto.tipo_movimiento,
                    cantidad = (int)dto.cantidad,
                    stock_antes = stockAntes,
                    stock_despues = stockDespues,
                    motivo = dto.motivo,
                    fecha = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    mensaje = "Movimiento registrado",
                    stock_actual = stockDespues,
                    estado =
                        stockDespues <= inventario.stock_minimo ? "BAJO" :
                        stockDespues >= inventario.stock_maximo ? "ALTO" :
                        "NORMAL"
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ─────────────────────────────────────────────
        // PUT /api/inventario/configuracion
        // ─────────────────────────────────────────────
        [HttpPut("configuracion")]
        public async Task<IActionResult> ConfigurarStock(InventarioDTOs dto)
        {
            var rolId = GetRolId();
            var sucursalId = GetSucursalId();

            var config = await _context.Inventario
                .Include(i => i.almacenes)
                .FirstOrDefaultAsync(i =>
                    i.producto_id == dto.producto_id &&
                    i.almacen_id == dto.almacen_id);

            if (config == null)
                return NotFound();

            // Admin solo puede configurar stock de su sucursal
            if (rolId != 0 && config.almacenes.sucursal_id != sucursalId)
                return Forbid();

            if (dto.stock_minimo < 0 || dto.stock_maximo < 0)
                return BadRequest("Valores inválidos");

            if (dto.stock_minimo > dto.stock_maximo)
                return BadRequest("Stock mínimo no puede ser mayor al máximo");

            if (dto.stock_maximo == 0)
                return BadRequest("Stock máximo no puede ser 0");

            config.stock_minimo = dto.stock_minimo;
            config.stock_maximo = dto.stock_maximo;
            config.updated_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Configuración actualizada",
                config.stock_minimo,
                config.stock_maximo
            });
        }

        // ─────────────────────────────────────────────
        // GET /api/inventario/kardex
        // ─────────────────────────────────────────────
        [HttpGet("kardex")]
        public async Task<IActionResult> GetKardex(
            int producto_id,
            int? almacen_id = null,
            DateTime? inicio = null,
            DateTime? fin = null)
        {
            var rolId = GetRolId();
            var sucursalId = GetSucursalId();

            var query = _context.InventarioMovimientos
                .Where(m => m.producto_id == producto_id);

            if (rolId != 0)
                query = query.Where(m => m.almacenes.sucursal_id == sucursalId);

            if (almacen_id.HasValue)
                query = query.Where(m => m.almacen_id == almacen_id);

            if (inicio.HasValue)
                query = query.Where(m => m.fecha >= inicio.Value);

            if (fin.HasValue)
                query = query.Where(m => m.fecha <= fin.Value);

            var data = await query
                .OrderByDescending(m => m.fecha)
                .Select(m => new
                {
                    m.fecha,
                    m.tipo_movimiento,
                    m.cantidad,
                    m.stock_antes,
                    m.stock_despues,
                    m.motivo,
                    producto = m.productos.nombre,
                    almacen = m.almacenes.nombre
                })
                .ToListAsync();

            return Ok(data);
        }
    }
}