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
                    nombre = i.productos.nombre,
                })
                .ToListAsync();

            return Ok(data);
        }


        [HttpPost]
        public async Task<IActionResult> RegistrarMovimiento(MovimientoInventarioDTOs dto)
        {
            if (dto.cantidad <= 0)
                return BadRequest("Cantidad inválida");

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

                switch (dto.tipo_movimiento)
                {
                    case "ENTRADA":
                        stockDespues += (int)dto.cantidad;
                        break;

                    case "SALIDA":
                        if (stockAntes < dto.cantidad)
                            return BadRequest("Stock insuficiente");

                        stockDespues -= (int)dto.cantidad;
                        break;

                    case "AJUSTE":
                        stockDespues = (int)dto.cantidad;
                        break;

                    default:
                        return BadRequest("Tipo inválido");
                }

                inventario.stock_actual = stockDespues;
                inventario.updated_at = DateTime.Now;

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
                    stock_actual = stockDespues
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

    }
}
