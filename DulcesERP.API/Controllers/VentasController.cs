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
    public class VentasController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public VentasController(DulcesERPContext context)
        {
            _context = context;
        }

        private int GetUsuarioId() => int.Parse(User.FindFirstValue("usuario_id")!);
        private int GetSucursalId() => int.Parse(User.FindFirstValue("sucursal_id")!);
        private int GetRolId() => int.Parse(User.FindFirstValue("rol_id")!);

        // ─────────────────────────────────────────────
        // GET /api/ventas
        // Historial de ventas de la sucursal
        // ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetVentas(
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null)
        {
            var sucursalId = GetSucursalId();
            var rolId = GetRolId();

            var query = _context.Ventas
                .AsNoTracking()
                .Include(v => v.clientes)
                .Include(v => v.usuarios)
                .Include(v => v.comprobantes)
                    .ThenInclude(c => c.tipos_comprobante)
                .Include(v => v.comprobantes)
                    .ThenInclude(c => c.series)
                .Include(v => v.pagos)
                    .ThenInclude(p => p.metodos_pago)
                .AsQueryable();

            // SuperAdmin ve todo, el resto solo su sucursal
            // Filtramos por usuario de esa sucursal
            if (rolId != 0)
            {
                var usuariosSucursal = await _context.Usuarios
                    .Where(u => u.sucursal_id == sucursalId)
                    .Select(u => u.usuario_id)
                    .ToListAsync();

                query = query.Where(v => usuariosSucursal.Contains(v.usuario_id));
            }

            if (desde.HasValue)
                query = query.Where(v => v.fecha >= desde.Value);
            if (hasta.HasValue)
                query = query.Where(v => v.fecha <= hasta.Value.AddDays(1));

            var ventas = await query
                .OrderByDescending(v => v.fecha)
                .Select(v => new
                {
                    v.venta_id,
                    v.pedido_id,
                    v.total,
                    v.fecha,
                    cliente = v.clientes.nombre,
                    cliente_doc = v.clientes.documento,
                    cajero = v.usuarios.nombre,
                    comprobante = v.comprobantes.Select(c => new
                    {
                        c.comprobante_id,
                        serie = c.series.serie,
                        c.numero,
                        tipo = c.tipos_comprobante.nombre,
                        codigo_sunat = c.tipos_comprobante.codigo_sunat,
                        c.subtotal,
                        c.igv,
                        c.total,
                        c.estado_sunat,
                        c.fecha
                    }).FirstOrDefault(),
                    pagos = v.pagos.Select(p => new
                    {
                        metodo = p.metodos_pago.nombre,
                        p.monto,
                        p.referencia
                    })
                })
                .ToListAsync();

            return Ok(ventas);
        }

        // ─────────────────────────────────────────────
        // GET /api/ventas/{id}
        // ─────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVenta(int id)
        {
            var venta = await _context.Ventas
                .AsNoTracking()
                .Include(v => v.clientes)
                .Include(v => v.comprobantes)
                    .ThenInclude(c => c.detalles)
                        .ThenInclude(d => d.productos)
                .Include(v => v.comprobantes)
                    .ThenInclude(c => c.series)
                .Include(v => v.comprobantes)
                    .ThenInclude(c => c.tipos_comprobante)
                .Include(v => v.pagos)
                    .ThenInclude(p => p.metodos_pago)
                .FirstOrDefaultAsync(v => v.venta_id == id);

            if (venta == null)
                return NotFound("Venta no encontrada");

            return Ok(venta);
        }

        // ─────────────────────────────────────────────
        // GET /api/ventas/pedidos-pendientes
        // Pedidos LISTO tipo PICKUP listos para cobrar
        // ─────────────────────────────────────────────
        [HttpGet("pedidos-pendientes")]
        public async Task<IActionResult> GetPedidosPendientes()
        {
            var sucursalId = GetSucursalId();

            var pedidos = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.sucursal_id == sucursalId
                         && p.estado_pedido_id == 4           // LISTO
                         && p.tipos_pedido == "PICKUP")
                .Include(p => p.clientes)
                .Include(p => p.pedido_detalle)
                    .ThenInclude(d => d.productos)
                .OrderBy(p => p.fecha)
                .Select(p => new
                {
                    p.pedido_id,
                    p.total,
                    p.fecha,
                    p.observaciones,
                    p.tipos_pedido,
                    p.metodo_pago,
                    p.pagado,
                    cliente = p.clientes.nombre,
                    cliente_doc = p.clientes.documento,
                    cliente_id = p.cliente_id,
                    detalles = p.pedido_detalle.Select(d => new
                    {
                        d.producto_id,
                        producto = d.productos.nombre,
                        d.cantidad,
                        d.precio,
                        d.subtotal
                    })
                })
                .ToListAsync();

            return Ok(pedidos);
        }

        // ─────────────────────────────────────────────
        // GET /api/ventas/metodos-pago
        // ─────────────────────────────────────────────
        [HttpGet("metodos-pago")]
        public async Task<IActionResult> GetMetodosPago()
        {
            var metodos = await _context.MetodosPago
                .AsNoTracking()
                .Where(m => m.activo == true)
                .Select(m => new { m.metodo_pago_id, m.nombre, m.codigo })
                .ToListAsync();

            return Ok(metodos);
        }

        // ─────────────────────────────────────────────
        // GET /api/ventas/tipos-comprobante
        // ─────────────────────────────────────────────
        [HttpGet("tipos-comprobante")]
        public async Task<IActionResult> GetTiposComprobante()
        {
            var tipos = await _context.TiposComprobante
                .AsNoTracking()
                .Where(t => t.codigo_sunat == "01" || t.codigo_sunat == "03")
                .Select(t => new
                {
                    t.tipo_comprobante_id,
                    t.codigo_sunat,
                    t.nombre
                })
                .ToListAsync();

            return Ok(tipos);
        }

        // ─────────────────────────────────────────────
        // POST /api/ventas
        // Crear venta, comprobante y pago en una transacción
        // ─────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> CrearVenta([FromBody] CrearVentaDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var usuarioId = GetUsuarioId();
                var sucursalId = GetSucursalId();

                // ── Obtener impuesto ──
                var impuesto = await _context.Impuestos
                    .FirstOrDefaultAsync(i => i.impuesto_id == dto.impuesto_id);

                if (impuesto == null)
                    return BadRequest("Impuesto no encontrado");

                decimal tasaIgv = impuesto.porcentaje / 100;

                // ── Calcular totales ──
                decimal totalVenta = 0;
                var detallesComprobante = new List<ComprobanteDetalle>();

                // Si viene de un pedido, usar sus detalles
                if (dto.pedido_id.HasValue && dto.detalles.Count == 0)
                {
                    var pedido = await _context.Pedidos
                        .Include(p => p.pedido_detalle)
                        .FirstOrDefaultAsync(p => p.pedido_id == dto.pedido_id);

                    if (pedido == null)
                        return BadRequest("Pedido no encontrado");

                    dto.detalles = pedido.pedido_detalle.Select(d => new DetalleVentaDTO
                    {
                        producto_id = d.producto_id,
                        cantidad = d.cantidad,
                        precio_unitario = d.precio
                    }).ToList();
                }

                if (dto.detalles.Count == 0)
                    return BadRequest("La venta debe tener al menos un producto");

                foreach (var item in dto.detalles)
                {
                    decimal totalItem = item.cantidad * item.precio_unitario;
                    decimal subtotalItem = Math.Round(totalItem / (1 + tasaIgv), 2);
                    decimal igvItem = Math.Round(totalItem - subtotalItem, 2);

                    totalVenta += totalItem;

                    detallesComprobante.Add(new ComprobanteDetalle
                    {
                        producto_id = item.producto_id,
                        cantidad = item.cantidad,
                        precio_unitario = item.precio_unitario,
                        subtotal = subtotalItem,
                        igv = igvItem,
                        total = totalItem
                    });
                }

                decimal subtotalTotal = Math.Round(totalVenta / (1 + tasaIgv), 2);
                decimal igvTotal = Math.Round(totalVenta - subtotalTotal, 2);

                // ── Crear venta ──
                var venta = new Ventas
                {
                    pedido_id = dto.pedido_id,
                    cliente_id = dto.cliente_id,
                    usuario_id = usuarioId,
                    impuesto_id = dto.impuesto_id,
                    total = totalVenta,
                    fecha = DateTime.UtcNow
                };

                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();

                // ── Obtener serie y correlativo ──
                var serie = await _context.SeriesComprobante
                    .FirstOrDefaultAsync(s =>
                        s.sucursal_id == sucursalId &&
                        s.tipo_comprobante_id == dto.tipo_comprobante_id &&
                        s.activo == true);

                if (serie == null)
                    return BadRequest(
                        $"No hay serie configurada para este tipo de comprobante " +
                        $"en esta sucursal");

                serie.correlativo_actual += 1;
                int numeroCorrelativo = serie.correlativo_actual;

                // ── Crear comprobante ──
                var comprobante = new Comprobantes
                {
                    venta_id = venta.venta_id,
                    serie_id = serie.serie_id,
                    numero = numeroCorrelativo,
                    tipo_comprobante_id = dto.tipo_comprobante_id,
                    cliente_id = dto.cliente_id,
                    impuesto_id = dto.impuesto_id,
                    subtotal = subtotalTotal,
                    igv = igvTotal,
                    total = totalVenta,
                    estado_sunat = "SIN_ENVIAR",
                    fecha = DateTime.UtcNow
                };

                _context.Comprobantes.Add(comprobante);
                await _context.SaveChangesAsync();

                // ── Crear detalles del comprobante ──
                foreach (var detalle in detallesComprobante)
                {
                    detalle.comprobante_id = comprobante.comprobante_id;
                    _context.ComprobanteDetalles.Add(detalle);
                }

                // ── Registrar pago ──
                _context.Pagos.Add(new Pagos
                {
                    venta_id = venta.venta_id,
                    metodo_pago_id = dto.metodo_pago_id,
                    monto = dto.monto_pagado,
                    referencia = dto.referencia_pago,
                    fecha = DateTime.UtcNow
                });

                // ── Actualizar estado del pedido y descontar stock ──
                if (dto.pedido_id.HasValue)
                {
                    var pedidoActualizar = await _context.Pedidos
                        .Include(p => p.pedido_detalle)
                        .FirstOrDefaultAsync(p => p.pedido_id == dto.pedido_id);

                    if (pedidoActualizar != null)
                    {
                        pedidoActualizar.estado_pedido_id = 5; // ENTREGADO
                        pedidoActualizar.pagado = true;
                        pedidoActualizar.metodo_pago = dto.referencia_pago ?? "POS";

                        // Descontar stock
                        foreach (var detalle in pedidoActualizar.pedido_detalle)
                        {
                            var inventario = await _context.Inventario
                                .Include(i => i.almacenes)
                                .FirstOrDefaultAsync(i =>
                                    i.producto_id == detalle.producto_id &&
                                    i.almacenes.sucursal_id == sucursalId);

                            if (inventario == null) continue;

                            int stockAntes = (int)inventario.stock_actual;
                            int stockDespues = Math.Max(0, stockAntes - detalle.cantidad);

                            inventario.stock_actual = stockDespues;
                            inventario.stock_reservado = Math.Max(
                                0, inventario.stock_reservado - detalle.cantidad);
                            inventario.updated_at = DateTime.UtcNow;

                            _context.InventarioMovimientos.Add(new InventarioMovimiento
                            {
                                producto_id = detalle.producto_id,
                                almacen_id = inventario.almacen_id,
                                tipo_movimiento = "SALIDA",
                                cantidad = detalle.cantidad,
                                referencia = pedidoActualizar.pedido_id,
                                stock_antes = stockAntes,
                                stock_despues = stockDespues,
                                motivo = $"Venta #{venta.venta_id} — " +
                                                  $"{comprobante.comprobante_id}",
                                fecha = DateTime.UtcNow
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // ── Respuesta ──
                return Ok(new
                {
                    mensaje = "Venta registrada correctamente",
                    venta_id = venta.venta_id,
                    comprobante_id = comprobante.comprobante_id,
                    serie = serie.serie,
                    numero = numeroCorrelativo,
                    numero_formato = $"{serie.serie}-{numeroCorrelativo:D8}",
                    subtotal = subtotalTotal,
                    igv = igvTotal,
                    total = totalVenta,
                    vuelto = dto.monto_pagado - totalVenta
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ─────────────────────────────────────────────
        // GET /api/ventas/resumen-dia
        // Dashboard del cajero
        // ─────────────────────────────────────────────
        [HttpGet("resumen-dia")]
        public async Task<IActionResult> GetResumenDia()
        {
            var sucursalId = GetSucursalId();
            var hoy = DateTime.UtcNow.Date;

            var usuariosSucursal = await _context.Usuarios
                .Where(u => u.sucursal_id == sucursalId)
                .Select(u => u.usuario_id)
                .ToListAsync();

            var ventas = await _context.Ventas
                .AsNoTracking()
                .Where(v => v.fecha >= hoy &&
                            usuariosSucursal.Contains(v.usuario_id))
                .Include(v => v.pagos)
                    .ThenInclude(p => p.metodos_pago)
                .ToListAsync();

            var resumen = new
            {
                total_ventas = ventas.Count,
                monto_total = ventas.Sum(v => v.total),
                por_metodo = ventas
                    .SelectMany(v => v.pagos)
                    .GroupBy(p => p.metodos_pago.nombre)
                    .Select(g => new
                    {
                        metodo = g.Key,
                        monto = g.Sum(p => p.monto),
                        count = g.Count()
                    })
            };

            return Ok(resumen);
        }
    }
}