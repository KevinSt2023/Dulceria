using DulcesERP.Application.Services;
using DulcesERP.Infrastructure.Context;
using DulcesERP.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        private static readonly Dictionary<int, int[]> _transicionesPermitidas = new()
        {
            { 1, new[] { 2, 6 } },  // PENDIENTE → CONFIRMADO o CANCELADO
            { 2, new[] { 3, 6 } },  // CONFIRMADO → EN_PREPARACION o CANCELADO
            { 3, new[] { 4, 6 } },  // EN_PREPARACION → LISTO o CANCELADO
            { 4, new[] { 5, 6 , 7} },  // LISTO → ENTREGADO o CANCELADO y DESPACHADO
            { 7, new[] { 5, 6 } },  // DESPACHADO → ENTREGADO o CANCELADO
             // ENTREGADO (5) y CANCELADO (6) son estados finales
        };

        public PedidosController(DulcesERPContext context)
        {
            _context = context;
        }

        private int GetUsuarioId() =>
            int.Parse(User.FindFirstValue("usuario_id")!);

        private int GetSucursalId() =>
            int.Parse(User.FindFirstValue("sucursal_id")!);

        // ─────────────────────────────────────────────
        // Helper: lee permite_pedido_sin_stock por sede
        // Si no hay config por sede, usa el valor global
        // ─────────────────────────────────────────────
        private async Task<bool> PermiteSinStock(int productoId, int sucursalId)
        {
            var configSede = await _context.ProductoSucursales
                .FirstOrDefaultAsync(ps =>
                    ps.producto_id == productoId &&
                    ps.sucursal_id == sucursalId);

            if (configSede != null)
                return configSede.permite_pedido_sin_stock;

            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.producto_id == productoId);

            return producto?.permite_pedido_sin_stock ?? true;
        }

        // ─────────────────────────────────────────────
        // GET /api/pedidos
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
                    p.pagado,
                    p.metodo_pago,
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

        // ─────────────────────────────────────────────
        // POST /api/pedidos — CREA Y RESERVA STOCK
        // ─────────────────────────────────────────────
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

                var usuarioId = GetUsuarioId();
                var sucursalId = GetSucursalId(); // sucursal del vendedor

                // ── Determinar sucursal de stock ──
                // PICKUP en otra sede → validar stock de esa sede
                // DELIVERY o PICKUP sin sede → validar stock de la sede del vendedor
                int sucursalStockId = sucursalId;

                if (dto.tipos_pedido == "PICKUP" && dto.sucursal_recojo_id.HasValue
                    && dto.sucursal_recojo_id.Value != 0)
                {
                    sucursalStockId = dto.sucursal_recojo_id.Value;
                }

                decimal total = 0;

                // ── Validar stock ANTES de crear el pedido ──
                var erroresStock = new List<string>();

                foreach (var item in dto.detalles)
                {
                    var producto = await _context.Productos
                        .FirstOrDefaultAsync(p => p.producto_id == (int)item.producto_id);

                    if (producto == null)
                        return BadRequest($"Producto {item.producto_id} no existe");

                    bool permiteSinStock = await PermiteSinStock(
                        producto.producto_id, sucursalStockId);

                    if (!permiteSinStock)
                    {
                        var inventario = await _context.Inventario
                            .Include(i => i.almacenes)
                            .FirstOrDefaultAsync(i =>
                                i.producto_id == producto.producto_id &&
                                i.almacenes.sucursal_id == sucursalStockId);

                        int disponible = inventario != null
                            ? (int)inventario.stock_actual - inventario.stock_reservado
                            : 0;

                        if (inventario == null)
                        {
                            erroresStock.Add(
                                $"❌ '{producto.nombre}' no tiene inventario " +
                                $"en la sede seleccionada");
                        }
                        else if (disponible < (int)item.cantidad)
                        {
                            erroresStock.Add(
                                $"❌ '{producto.nombre}': " +
                                $"disponible {disponible}, solicitado {(int)item.cantidad}");
                        }
                    }
                }

                // Si hay errores de stock, retornar TODOS juntos
                if (erroresStock.Any())
                {
                    var sucursal = await _context.Sucursales
                        .FirstOrDefaultAsync(s => s.sucursal_id == sucursalStockId);

                    return BadRequest(new
                    {
                        mensaje = $"Stock insuficiente en {sucursal?.nombre ?? "la sede seleccionada"}",
                        errores = erroresStock,
                        sucursal = sucursal?.nombre,
                        sugerencia = "Cambia la sede de recojo o ajusta las cantidades"
                    });
                }

                // ── Crear pedido ──
                var pedido = new Pedidos
                {
                    cliente_id = dto.cliente_id,
                    usuario_id = usuarioId,
                    sucursal_id = sucursalStockId, // ← sede donde se prepara
                    estado_pedido_id = estadoPendiente.estado_pedido_id,
                    observaciones = dto.observaciones,
                    direccion_entrega = dto.direccion_entrega,
                    tipos_pedido = dto.tipos_pedido,
                    pagado = dto.pagado,
                    metodo_pago = dto.metodo_pago,
                    fecha = DateTime.UtcNow
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // ── Crear detalles y reservar stock ──
                foreach (var item in dto.detalles)
                {
                    var producto = await _context.Productos
                        .FirstOrDefaultAsync(p => p.producto_id == (int)item.producto_id);

                    decimal precio = producto!.precio ?? 0m;
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

                    bool permiteSinStock = await PermiteSinStock(
                        producto.producto_id, sucursalStockId);

                    if (!permiteSinStock)
                    {
                        var inventario = await _context.Inventario
                            .Include(i => i.almacenes)
                            .FirstOrDefaultAsync(i =>
                                i.producto_id == producto.producto_id &&
                                i.almacenes.sucursal_id == sucursalStockId);

                        if (inventario != null)
                        {
                            inventario.stock_reservado += cantidad;
                            inventario.updated_at = DateTime.UtcNow;
                        }
                    }
                }

                pedido.total = total;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    mensaje = "Pedido creado correctamente",
                    pedido_id = pedido.pedido_id,
                    total,
                    sucursal_preparacion = (await _context.Sucursales
                        .FirstOrDefaultAsync(s => s.sucursal_id == sucursalStockId))?.nombre
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ─────────────────────────────────────────────
        // PUT /api/pedidos/{id}/estado
        // ─────────────────────────────────────────────
        [HttpPut("{id}/estado")]
        public async Task<IActionResult> CambiarEstado(int id, [FromBody] int estado_id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var sucursalId = GetSucursalId();
                var rolId = int.Parse(User.FindFirstValue("rol_id")!);

                var pedido = await _context.Pedidos
                    .Include(p => p.pedido_detalle)
                    .FirstOrDefaultAsync(p =>
                        p.pedido_id == id && p.sucursal_id == sucursalId);

                if (pedido == null)
                    return NotFound("Pedido no encontrado");

                bool permitido = rolId switch
                {
                    0 => true,                              // SuperAdmin — todo
                    1 => true,                              // Admin — todo
                    3 => estado_id == 2 || estado_id == 3 || estado_id == 4,// Produccion: CONFIRMADO, EN_PREPARACION
                    4 => estado_id == 4 || estado_id == 5, // Cajero: LISTO, ENTREGADO
                    5 => estado_id == 5 || estado_id == 7, // ← Distribuidor: ENTREGADO y DESPACHADO
                    _ => false
                };

                if (!permitido)
                    return Forbid();

                if (!_transicionesPermitidas.TryGetValue(
                        pedido.estado_pedido_id, out var permitidos)
                    || !permitidos.Contains(estado_id))
                {
                    return BadRequest(
                        $"Transición no permitida: " +
                        $"{pedido.estado_pedido_id} → {estado_id}");
                }

                var estado = await _context.EstadosPedido
                    .FirstOrDefaultAsync(e => e.estado_pedido_id == estado_id);

                if (estado == null)
                    return BadRequest("Estado inválido");

                // ── ENTREGADO (5): descontar stock y liberar reserva ──
                if (estado_id == 5)
                {
                    foreach (var detalle in pedido.pedido_detalle)
                    {
                        bool permiteSinStock = await PermiteSinStock(
                            detalle.producto_id, sucursalId);

                        if (permiteSinStock) continue;

                        var inventario = await _context.Inventario
                            .Include(i => i.almacenes)
                            .FirstOrDefaultAsync(i =>
                                i.producto_id == detalle.producto_id &&
                                i.almacenes.sucursal_id == sucursalId);

                        if (inventario == null) continue; // ← antes retornaba error, ahora continúa

                        int stockAntes = (int)inventario.stock_actual;
                        // ← Usar Math.Max para nunca quedar negativo
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

        // ─────────────────────────────────────────────
        // PUT /api/pedidos/{id}/cancelar — LIBERA RESERVA
        // ─────────────────────────────────────────────
        [HttpPut("{id}/cancelar")]
        public async Task<IActionResult> CancelarPedido(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var sucursalId = GetSucursalId();

                var pedido = await _context.Pedidos
                    .Include(p => p.pedido_detalle)
                    .FirstOrDefaultAsync(p =>
                        p.pedido_id == id && p.sucursal_id == sucursalId);

                if (pedido == null)
                    return NotFound("Pedido no encontrado");

                if (pedido.estado_pedido_id == 5)
                    return BadRequest("No se puede cancelar un pedido ya entregado");

                if (pedido.estado_pedido_id == 6)
                    return BadRequest("El pedido ya está cancelado");

                var estadoCancelado = await _context.EstadosPedido
                    .FirstOrDefaultAsync(e => e.nombre == "CANCELADO");

                if (estadoCancelado == null)
                    return BadRequest("Estado CANCELADO no configurado en BD");

                // ── Liberar reservas ──
                foreach (var detalle in pedido.pedido_detalle)
                {
                    // ← Leer por sede
                    bool permiteSinStock = await PermiteSinStock(
                        detalle.producto_id, sucursalId);

                    if (permiteSinStock) continue;

                    var inventario = await _context.Inventario
                        .Include(i => i.almacenes)
                        .FirstOrDefaultAsync(i =>
                            i.producto_id == detalle.producto_id &&
                            i.almacenes.sucursal_id == sucursalId);

                    if (inventario == null) continue;

                    inventario.stock_reservado = Math.Max(
                        0, inventario.stock_reservado - detalle.cantidad);
                    inventario.updated_at = DateTime.UtcNow;
                }

                pedido.estado_pedido_id = estadoCancelado.estado_pedido_id;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { mensaje = "Pedido cancelado" });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpPut("{id}/entregar")]
        public async Task<IActionResult> EntregarPedido(int id, [FromBody] EntregarPedidoDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sucursalId = GetSucursalId();
                var rolId = int.Parse(User.FindFirstValue("rol_id")!);

                if (rolId != 0 && rolId != 1 && rolId != 5)
                    return Forbid();

                var pedido = await _context.Pedidos
                    .Include(p => p.pedido_detalle)
                    .FirstOrDefaultAsync(p =>
                        p.pedido_id == id &&
                        p.sucursal_id == sucursalId);

                if (pedido == null)
                    return NotFound("Pedido no encontrado");

                if (pedido.estado_pedido_id != 4 && pedido.estado_pedido_id != 7)
                    return BadRequest("El pedido no está en estado LISTO o DESPACHADO");

                // Registrar cobro — CONTADO o CRÉDITO
                pedido.metodo_pago = dto.metodo_pago;
                pedido.monto_pagado = dto.monto_cobrado;
                pedido.tipo_pago = dto.tipo_pago;

                if (dto.tipo_pago == "CREDITO")
                {
                    pedido.saldo_pendiente = pedido.total - dto.monto_cobrado;
                    pedido.pagado = pedido.saldo_pendiente <= 0;
                }
                else
                {
                    pedido.saldo_pendiente = 0;
                    pedido.pagado = true;
                }

                pedido.estado_pedido_id = 5; // ENTREGADO

                // Registrar abono inicial
                _context.Abonos.Add(new Abono
                {
                    pedido_id = pedido.pedido_id,
                    monto = dto.monto_cobrado,
                    metodo_pago = dto.metodo_pago,
                    observacion = dto.tipo_pago == "CREDITO" ? "Pago inicial" : "Pago completo",
                    usuario_id = int.Parse(User.FindFirstValue("usuario_id")!),
                    fecha = DateTime.UtcNow
                });

                // Descontar stock
                foreach (var detalle in pedido.pedido_detalle)
                {
                    bool permiteSinStock = await PermiteSinStock(
                        detalle.producto_id, sucursalId);

                    if (permiteSinStock) continue;

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
                        referencia = pedido.pedido_id,
                        stock_antes = stockAntes,
                        stock_despues = stockDespues,
                        motivo = $"Entrega pedido #{pedido.pedido_id}",
                        fecha = DateTime.UtcNow
                    });
                }

                // ── Generar venta y comprobante automático (Boleta por defecto) ──
                var clienteGenerico = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.documento == "00000000");

                var clienteId = pedido.cliente_id > 0
                    ? pedido.cliente_id
                    : clienteGenerico?.cliente_id ?? 0;

                var impuesto = await _context.Impuestos
                    .FirstOrDefaultAsync(i => i.impuesto_id == 1);

                if (impuesto != null && clienteId > 0)
                {
                    decimal tasaIgv = impuesto.porcentaje / 100;

                    // Calcular totales
                    decimal totalVenta = pedido.total;
                    decimal subtotalTotal = Math.Round(totalVenta / (1 + tasaIgv), 2);
                    decimal igvTotal = Math.Round(totalVenta - subtotalTotal, 2);

                    // Crear venta
                    var venta = new Ventas
                    {
                        pedido_id = pedido.pedido_id,
                        cliente_id = clienteId,
                        usuario_id = int.Parse(User.FindFirstValue("usuario_id")!),
                        impuesto_id = 1,
                        total = totalVenta,
                        fecha = DateTime.UtcNow
                    };
                    _context.Ventas.Add(venta);
                    await _context.SaveChangesAsync();

                    // Obtener serie boleta de la sucursal
                    var tipoBoletaId = await _context.TiposComprobante
                        .Where(t => t.codigo_sunat == "03")
                        .Select(t => t.tipo_comprobante_id)
                        .FirstOrDefaultAsync();

                    var serie = await _context.SeriesComprobante
                        .FirstOrDefaultAsync(s =>
                            s.sucursal_id == sucursalId &&
                            s.tipo_comprobante_id == tipoBoletaId &&
                            s.activo == true);

                    if (serie != null)
                    {
                        serie.correlativo_actual += 1;

                        var comprobante = new Comprobantes
                        {
                            venta_id = venta.venta_id,
                            serie_id = serie.serie_id,
                            numero = serie.correlativo_actual,
                            tipo_comprobante_id = tipoBoletaId,
                            cliente_id = clienteId,
                            impuesto_id = 1,
                            subtotal = subtotalTotal,
                            igv = igvTotal,
                            total = totalVenta,
                            estado_sunat = "SIN_ENVIAR",
                            fecha = DateTime.UtcNow
                        };
                        _context.Comprobantes.Add(comprobante);
                        await _context.SaveChangesAsync();

                        // Detalles del comprobante
                        foreach (var detalle in pedido.pedido_detalle)
                        {
                            decimal totalItem = detalle.precio * detalle.cantidad;
                            decimal subtotalItem = Math.Round(totalItem / (1 + tasaIgv), 2);
                            decimal igvItem = Math.Round(totalItem - subtotalItem, 2);

                            _context.ComprobanteDetalles.Add(new ComprobanteDetalle
                            {
                                comprobante_id = comprobante.comprobante_id,
                                producto_id = detalle.producto_id,
                                cantidad = detalle.cantidad,
                                precio_unitario = detalle.precio,
                                subtotal = subtotalItem,
                                igv = igvItem,
                                total = totalItem
                            });
                        }

                        // Registrar pago
                        var metodo = await _context.MetodosPago
                            .FirstOrDefaultAsync(m => m.nombre.ToLower()
                                .Contains(dto.metodo_pago.ToLower()) ||
                                m.codigo.ToLower() == dto.metodo_pago.ToLower());

                        if (metodo != null)
                        {
                            _context.Pagos.Add(new Pagos
                            {
                                venta_id = venta.venta_id,
                                metodo_pago_id = metodo.metodo_pago_id,
                                monto = dto.monto_cobrado,
                                fecha = DateTime.UtcNow
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    mensaje = dto.tipo_pago == "CREDITO" ? "Pedido entregado con crédito" : "Pedido entregado y cobrado",
                    monto_cobrado = dto.monto_cobrado,
                    metodo_pago = dto.metodo_pago,
                    tipo_pago = dto.tipo_pago,
                    saldo_pendiente = pedido.saldo_pendiente,
                    vuelto = dto.tipo_pago == "CONTADO" ? dto.monto_cobrado - pedido.total : 0
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ── POST /api/pedidos/{id}/abonar ────────────────────────
        [HttpPost("{id}/abonar")]
        public async Task<IActionResult> AbonarPedido(int id, [FromBody] AbonoDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sucursalId = GetSucursalId();
                var pedido = await _context.Pedidos
                    .FirstOrDefaultAsync(p => p.pedido_id == id && p.sucursal_id == sucursalId);

                if (pedido == null)
                    return NotFound("Pedido no encontrado");

                if (pedido.estado_pedido_id != 5)
                    return BadRequest("Solo se pueden abonar pedidos entregados");

                if (pedido.saldo_pendiente <= 0)
                    return BadRequest("Este pedido no tiene saldo pendiente");

                if (dto.monto <= 0 || dto.monto > pedido.saldo_pendiente)
                    return BadRequest($"Monto inválido. Saldo pendiente: S/ {pedido.saldo_pendiente}");

                pedido.monto_pagado += dto.monto;
                pedido.saldo_pendiente = Math.Max(0, pedido.saldo_pendiente - dto.monto);
                pedido.pagado = pedido.saldo_pendiente <= 0;

                _context.Abonos.Add(new Abono
                {
                    pedido_id = pedido.pedido_id,
                    monto = dto.monto,
                    metodo_pago = dto.metodo_pago,
                    observacion = dto.observacion,
                    usuario_id = GetUsuarioId(),
                    fecha = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    mensaje = pedido.pagado ? "Deuda cancelada completamente" : "Abono registrado",
                    monto_abonado = dto.monto,
                    saldo_pendiente = pedido.saldo_pendiente,
                    pagado_completo = pedido.pagado
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ── GET /api/pedidos/{id}/abonos ─────────────────────────
        [HttpGet("{id}/abonos")]
        public async Task<IActionResult> GetAbonos(int id)
        {
            var sucursalId = GetSucursalId();

            var pedido = await _context.Pedidos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.pedido_id == id && p.sucursal_id == sucursalId);

            if (pedido == null)
                return NotFound();

            var abonos = await _context.Abonos
                .AsNoTracking()
                .Where(a => a.pedido_id == id)
                .OrderBy(a => a.fecha)
                .Select(a => new
                {
                    a.abono_id,
                    a.monto,
                    a.metodo_pago,
                    a.observacion,
                    a.fecha
                })
                .ToListAsync();

            return Ok(new
            {
                pedido_id = pedido.pedido_id,
                total = pedido.total,
                monto_pagado = pedido.monto_pagado,
                saldo_pendiente = pedido.saldo_pendiente,
                pagado = pedido.pagado,
                tipo_pago = pedido.tipo_pago,
                abonos
            });
        }

        // GET /api/pedidos/creditos-pendientes
        [HttpGet("creditos-pendientes")]
        public async Task<IActionResult> GetCreditosPendientes()
        {
            var sucursalId = GetSucursalId();

            var pedidos = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.sucursal_id == sucursalId
                         && p.estado_pedido_id == 5  // ENTREGADO
                         && p.saldo_pendiente > 0)
                .Include(p => p.clientes)
                .Include(p => p.pedido_detalle)
                    .ThenInclude(d => d.productos)
                .OrderByDescending(p => p.fecha)
                .Select(p => new
                {
                    p.pedido_id,
                    p.total,
                    p.monto_pagado,
                    p.saldo_pendiente,
                    p.metodo_pago,
                    p.tipo_pago,
                    p.fecha,
                    cliente = p.clientes.nombre,
                    cliente_doc = p.clientes.documento,
                    detalles = p.pedido_detalle.Select(d => new
                    {
                        producto = d.productos.nombre,
                        d.cantidad,
                        d.precio,
                        d.subtotal
                    })
                })
                .ToListAsync();

            return Ok(pedidos);
        }
    }
}