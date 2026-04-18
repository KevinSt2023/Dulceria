using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DulcesERP.Infrastructure.Context;
using DulcesERP.Domain.Entities;
using DulcesERP.Application.DTOs;
using System.Security.Claims;

namespace DulcesERP.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PedidosController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        // Transiciones válidas: desde un estado, a cuáles puede avanzar
        private static readonly Dictionary<int, int[]> _transicionesPermitidas = new()
        {
            { 1, new[] { 2, 6 } },      // PENDIENTE       → CONFIRMADO, CANCELADO
            { 2, new[] { 3, 6 } },      // CONFIRMADO      → EN_PREPARACION, CANCELADO
            { 3, new[] { 4, 6 } },      // EN_PREPARACION  → LISTO, CANCELADO
            { 4, new[] { 5, 6 } },      // LISTO           → ENTREGADO, CANCELADO
            // ENTREGADO y CANCELADO son estados finales, no se pueden mover
        };

        public PedidosController(DulcesERPContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────
        // HELPERS — leer claims del JWT
        // ─────────────────────────────────────────────

        private int GetUsuarioId() =>
            int.Parse(User.FindFirstValue("usuario_id")!);

        private int GetSucursalId() =>
            int.Parse(User.FindFirstValue("sucursal_id")!);


        // ─────────────────────────────────────────────
        // GET /api/pedidos
        // Solo devuelve pedidos de la sucursal del usuario logueado
        // ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetPedidos()
        {
            var sucursalId = GetSucursalId();

            var data = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.sucursal_id == sucursalId)
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
                    p.observaciones,
                    p.direccion_entrega,
                    p.tipos_pedido,
                    detalles = p.pedido_detalle.Select(d => new
                    {
                        producto = d.productos.nombre,
                        d.cantidad,
                        d.precio,
                        d.subtotal,
                        d.descuento
                    })
                })
                .ToListAsync();

            return Ok(data);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetPedido(int id)
        {
            var sucursalId = GetSucursalId();

            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.pedido_id == id && p.sucursal_id == sucursalId)
                .Include(p => p.clientes)
                .Include(p => p.estados_pedidos)
                .Include(p => p.pedido_detalle)
                    .ThenInclude(d => d.productos)
                .FirstOrDefaultAsync();

            if (pedido == null)
                return NotFound("Pedido no encontrado");

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
                    return BadRequest("Estado inicial no configurado en BD");

                // Tomar del JWT — no del body
                var usuarioId = GetUsuarioId();
                var sucursalId = GetSucursalId();

                decimal total = 0;

                var pedido = new Pedidos
                {
                    cliente_id = dto.cliente_id,
                    usuario_id = usuarioId,
                    sucursal_id = sucursalId,
                    estado_pedido_id = estadoPendiente.estado_pedido_id,
                    observaciones = dto.observaciones,
                    direccion_entrega = dto.direccion_entrega,
                    tipos_pedido = dto.tipos_pedido,
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

                    _context.PedidoDetalles.Add(new PedidoDetalle
                    {
                        pedido_id = pedido.pedido_id,
                        producto_id = producto.producto_id,
                        cantidad = cantidad,
                        precio = precio,
                        subtotal = subtotal
                    });
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
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                bool permitido = false;
                var sucursalId = GetSucursalId();
                var rolId = int.Parse(User.FindFirstValue("rol_id")!);
                var pedido = await _context.Pedidos
                    .Include(p => p.pedido_detalle)
                    .FirstOrDefaultAsync(p => p.pedido_id == id && p.sucursal_id == sucursalId);

                switch (rolId)
                {
                    case 0:
                        permitido = true; break;
                        // Superadmin
                    case 1: // Admin
                        permitido = true; break;

                    case 3: // Produccion
                        permitido = estado_id == 2 || estado_id == 3;
                        break;

                    case 4: // Cajero
                        permitido = estado_id == 4 || estado_id == 5;
                        break;

                    case 5: // Distribuidor
                        permitido = estado_id == 5;
                        break;
                }

                if (!permitido)
                    return Forbid("No tienes permiso para cambiar a este estado");


                if (pedido == null)
                    return NotFound("Pedido no encontrado");

                // Validar que la transición esté permitida
                if (!_transicionesPermitidas.TryGetValue(pedido.estado_pedido_id, out var permitidos)
                    || !permitidos.Contains(estado_id))
                {
                    return BadRequest($"Transición de estado no permitida: {pedido.estado_pedido_id} → {estado_id}");
                }

                var estado = await _context.EstadosPedido
                    .FirstOrDefaultAsync(e => e.estado_pedido_id == estado_id);

                if (estado == null)
                    return BadRequest("Estado inválido");

                // ── Descuento de stock al marcar ENTREGADO (estado_id = 5) ──
                if (estado_id == 5)
                {
                    foreach (var detalle in pedido.pedido_detalle)
                    {
                        // Busca el inventario del producto en el almacén de la sucursal
                        var inventario = await _context.Inventario
                            .Include(i => i.almacenes)
                            .FirstOrDefaultAsync(i =>
                                i.producto_id == detalle.producto_id &&
                                i.almacenes.sucursal_id == sucursalId);

                        if (inventario == null)
                            return BadRequest($"Sin registro de inventario para producto {detalle.producto_id}");

                        if (inventario.stock_actual < detalle.cantidad)
                            return BadRequest($"Stock insuficiente para producto {detalle.producto_id}. " +
                                              $"Disponible: {inventario.stock_actual}, requerido: {detalle.cantidad}");

                        int stockAntes = inventario.stock_actual;
                        int stockDespues = stockAntes - detalle.cantidad;

                        inventario.stock_actual = stockDespues;
                        inventario.updated_at = DateTime.UtcNow;

                        // Registra el movimiento de salida
                        _context.InventarioMovimientos.Add(new InventarioMovimiento
                        {
                            producto_id = detalle.producto_id,
                            almacen_id = inventario.almacen_id,
                            tipo_movimiento = "SALIDA",
                            cantidad = detalle.cantidad,
                            referencia = pedido.pedido_id,
                            stock_antes = stockAntes,
                            stock_despues = stockDespues,
                            motivo = $"Entrega pedido #{pedido.pedido_id}",
                            fecha = DateTime.UtcNow
                        });
                    }
                }

                pedido.estado_pedido_id = estado_id;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    mensaje = "Estado actualizado",
                    estado = estado.nombre
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        [HttpPut("{id}/cancelar")]
        public async Task<IActionResult> CancelarPedido(int id)
        {
            var sucursalId = GetSucursalId();

            var pedido = await _context.Pedidos
                .FirstOrDefaultAsync(p => p.pedido_id == id && p.sucursal_id == sucursalId);

            if (pedido == null)
                return NotFound("Pedido no encontrado");

            // No se puede cancelar lo que ya está entregado o ya cancelado
            if (pedido.estado_pedido_id == 5)
                return BadRequest("No se puede cancelar un pedido ya entregado");

            if (pedido.estado_pedido_id == 6)
                return BadRequest("El pedido ya está cancelado");

            var estadoCancelado = await _context.EstadosPedido
                .FirstOrDefaultAsync(e => e.nombre == "CANCELADO");

            if (estadoCancelado == null)
                return BadRequest("Estado CANCELADO no configurado en BD");

            pedido.estado_pedido_id = estadoCancelado.estado_pedido_id;

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Pedido cancelado" });
        }
    }
}