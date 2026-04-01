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
    public class InventarioController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public InventarioController(DulcesERPContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetInventario()
        {
            var data = await _context.Inventario
                .Include(i => i.productos)
                .Select(i => new
                {
                    i.producto_id,
                    i.almacen_id,
                    i.stock_actual,
                    i.stock_minimo,
                    i.stock_maximo,
                    nombreproducto = i.productos.nombre,
                    almacennombre = i.almacenes.nombre,

                    estado =
                    i.stock_actual <= i.stock_minimo ? "BAJO" :
                    i.stock_actual >= i.stock_maximo ? "ALTO" :
                    "NORMAL"
                })
                .OrderBy(i => i.producto_id)
                .ToListAsync();

            return Ok(data);
        }


        [HttpPost]
        public async Task<IActionResult> RegistrarMovimiento(MovimientoInventarioDTOs dto)
        {
            // 🔥 VALIDACIONES INICIALES
            if (dto.cantidad <= 0)
                return BadRequest("Cantidad inválida");

            if (string.IsNullOrWhiteSpace(dto.motivo))
                return BadRequest("Debe ingresar un motivo");

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
                        stock_minimo = 0,
                        stock_maximo = 0,
                        updated_at = DateTime.Now
                    };

                    _context.Inventario.Add(inventario);
                }

                var stockAntes = inventario.stock_actual;
                var stockDespues = stockAntes;

                // 🔥 VALIDACIONES DE EXISTENCIA
                var productoExiste = await _context.Productos
                    .AnyAsync(p => p.producto_id == dto.producto_id);

                var almacenExiste = await _context.Almacenes
                    .AnyAsync(a => a.almacen_id == dto.almacen_id);

                if (!productoExiste)
                    return BadRequest("Producto no existe");

                if (!almacenExiste)
                    return BadRequest("Almacén no existe");

                // 🔥 VALIDAR TIPO
                var tiposValidos = new[] { "ENTRADA", "SALIDA", "AJUSTE" };

                if (!tiposValidos.Contains(dto.tipo_movimiento))
                    return BadRequest("Tipo de movimiento inválido");

                // 🔥 REGLAS DE NEGOCIO
                if (dto.tipo_movimiento == "SALIDA" && inventario.stock_actual < dto.cantidad)
                    return BadRequest("Stock insuficiente");

                // 🔥 LÓGICA
                switch (dto.tipo_movimiento)
                {
                    case "ENTRADA":
                        stockDespues += (int)dto.cantidad;
                        break;

                    case "SALIDA":
                        stockDespues -= (int)dto.cantidad;
                        break;

                    case "AJUSTE":
                        stockDespues = (int)dto.cantidad;
                        break;
                }

                // 🔥 SEGURIDAD FINAL (doble validación)
                if (stockDespues < 0)
                    return BadRequest("Stock no puede ser negativo");

                // 🔥 ACTUALIZAR
                inventario.stock_actual = stockDespues;
                inventario.updated_at = DateTime.Now;

                // 📜 KARDEX
                var movimiento = new InventarioMovimiento
                {
                    producto_id = dto.producto_id,
                    almacen_id = dto.almacen_id,
                    tipo_movimiento = dto.tipo_movimiento,
                    cantidad = (int)dto.cantidad,
                    stock_antes = stockAntes,
                    stock_despues = stockDespues,
                    motivo = dto.motivo,
                    fecha = DateTime.Now
                };

                _context.InventarioMovimientos.Add(movimiento);

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


        [HttpPut("configuracion")]
        public async Task<IActionResult> ConfigurarStock(InventarioDTOs dto)
        {
            var config = await _context.Inventario.FirstOrDefaultAsync(i =>
                i.producto_id == dto.producto_id &&
                i.almacen_id == dto.almacen_id);

            if (config == null)
                return NotFound();

            if (dto.stock_minimo < 0 || dto.stock_maximo < 0)
                return BadRequest("Valores inválidos");

            if (dto.stock_minimo > dto.stock_maximo)
                return BadRequest("Stock mínimo no puede ser mayor al máximo");

            if (dto.stock_maximo == 0)
                return BadRequest("Stock máximo no puede ser 0");

            config.stock_minimo = dto.stock_minimo;
            config.stock_maximo = dto.stock_maximo;
            config.updated_at = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Configuración actualizada",
                config.stock_minimo,
                config.stock_maximo
            });
        }

    }
}
