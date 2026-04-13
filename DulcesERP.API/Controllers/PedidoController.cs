using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DulcesERP.Infrastructure.Context;
using DulcesERP.Domain.Entities;
using DulcesERP.Application.DTOs;

namespace DulcesERP.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PedidosController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public PedidosController(DulcesERPContext context)
        {
            _context = context;
        }

        
        [HttpGet]
        public async Task<IActionResult> GetPedidos()
        {
            var data = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.clientes)
                .Include(p => p.estados_pedidos)
                .Include(p => p.sucursales)
                .Include(p => p.pedido_detalle)
                    .ThenInclude(d => d.productos)
                .OrderByDescending(p => p.fecha)
                .Select(p => new
                {
                    p.pedido_id,
                    estado_id = p.estado_pedido_id,
                    cliente = p.clientes.nombre,
                    estado = p.estados_pedidos.nombre,
                    sucursal = p.sucursales.nombre,
                    p.total,
                    p.fecha,
                    detalles = p.pedido_detalle.Select(d => new
                    {
                        producto = d.productos.nombre,
                        d.cantidad,
                        d.precio,
                        d.subtotal
                    })
                })
                .ToListAsync();

            return Ok(data);
        }

       
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPedido(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.clientes)
                .Include(p => p.estados_pedidos)
                .Include(p => p.pedido_detalle)
                    .ThenInclude(d => d.productos)
                .FirstOrDefaultAsync(p => p.pedido_id == id);

            if (pedido == null)
                return NotFound();

            return Ok(pedido);
        }

        
        [HttpPost]
        public async Task<IActionResult> CrearPedido([FromBody] CrearPedidoDTOs dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (dto.detalles == null || dto.detalles.Count == 0)
                    return BadRequest("El pedido debe tener al menos un producto");

                var estadoPendiente = await _context.EstadosPedido
                    .FirstOrDefaultAsync(e => e.nombre == "PENDIENTE");

                if (estadoPendiente == null)
                    return BadRequest("Estado inicial no configurado");

                decimal total = 0;

                var pedido = new Pedidos
                {
                    cliente_id = dto.cliente_id,
                    usuario_id = dto.usuario_id,
                    sucursal_id = dto.sucursal_id,
                    estado_pedido_id = estadoPendiente.estado_pedido_id,
                    fecha = DateTime.UtcNow
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                foreach (var item in dto.detalles)
                {
                    var producto = await _context.Productos
                        .FirstOrDefaultAsync(p => p.producto_id == (int)item.producto_id);

                    if (producto == null)
                        return BadRequest($"Producto {item.producto_id} no existe");

                    decimal precio = producto.precio ?? 0m;
                    int cantidad = (int)item.cantidad;
                    decimal subtotal = precio * cantidad;

                    total += subtotal;

                    var detalle = new PedidoDetalle
                    {
                        pedido_id = pedido.pedido_id,
                        producto_id = producto.producto_id,
                        cantidad = cantidad,
                        precio = precio,
                        subtotal = subtotal
                    };

                    _context.PedidoDetalles.Add(detalle);
                }

                pedido.total = total;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    mensaje = "Pedido creado correctamente",
                    pedido_id = pedido.pedido_id,
                    total
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpPut("{id}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] int estado_id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);

            if (pedido == null)
                return NotFound();

            var estado = await _context.EstadosPedido
                .FirstOrDefaultAsync(e => e.estado_pedido_id == estado_id);

            if (estado == null)
                return BadRequest("Estado inválido");

            pedido.estado_pedido_id = estado_id;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Estado actualizado",
                estado = estado.nombre
            });
        }

        [HttpPut("{id}/cancelar")]
        public async Task<IActionResult> CancelarPedido(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);

            if (pedido == null)
                return NotFound();

            var estadoCancelado = await _context.EstadosPedido
                .FirstOrDefaultAsync(e => e.nombre == "CANCELADO");

            if (estadoCancelado == null)
                return BadRequest("Estado CANCELADO no existe");

            pedido.estado_pedido_id = estadoCancelado.estado_pedido_id;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Pedido cancelado"
            });
        }
    }
}
