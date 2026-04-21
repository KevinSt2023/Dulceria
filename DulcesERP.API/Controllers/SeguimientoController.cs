using DulcesERP.Application.DTOs;
using DulcesERP.Infrastructure.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static DulcesERP.Application.DTOs.SeguimientoDTO;

namespace DulcesERP.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SeguimientoController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public SeguimientoController(DulcesERPContext context)
        {
            _context = context;
        }

        private int GetSucursalId() =>
            int.Parse(User.FindFirstValue("sucursal_id")!);

        private int GetRolId() =>
            int.Parse(User.FindFirstValue("rol_id")!);

        [HttpGet]
        public async Task<IActionResult> GetCola()
        {
            var sucursalId = GetSucursalId();
            var rolId = GetRolId();

            if (rolId != 0 && rolId != 1 && rolId != 3 && rolId != 5)
                return Forbid();

            int[] estadosVisibles;

            if (rolId == 5)
                estadosVisibles = new[] { 4 };
            else if (rolId == 3)
                estadosVisibles = new[] { 1, 2, 3 };
            else
                estadosVisibles = new[] { 1, 2, 3, 4 };

            var pedidos = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.sucursal_id == sucursalId
                         && estadosVisibles.Contains(p.estado_pedido_id))
                .Include(p => p.clientes)
                .Include(p => p.estados_pedidos)
                .Include(p => p.pedido_detalle)
                    .ThenInclude(d => d.productos)
                .OrderBy(p => p.fecha)
                .ToListAsync();

            var productoIds = pedidos
                .SelectMany(p => p.pedido_detalle)
                .Select(d => d.producto_id)
                .Distinct()
                .ToList();

            // ← Cast explícito a int para evitar mezcla decimal/int
            var stockMap = await _context.Inventario
                .AsNoTracking()
                .Where(i => productoIds.Contains(i.producto_id)
                         && i.almacenes.sucursal_id == sucursalId)
                .Select(i => new
                {
                    i.producto_id,
                    stock_actual = (int)i.stock_actual,
                    i.stock_reservado,
                    stock_disponible = (int)i.stock_actual - i.stock_reservado
                })
                .ToListAsync();

            var stockDict = stockMap
                .GroupBy(s => s.producto_id)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(s => s.stock_disponible)
                );

            var resultado = pedidos.Select(p => new SeguimientoItemDTO
            {
                pedido_id = p.pedido_id,
                cliente = p.clientes.nombre,
                estado = p.estados_pedidos.nombre,
                estado_id = p.estado_pedido_id,
                tipos_pedido = p.tipos_pedido ?? "",
                direccion_entrega = p.direccion_entrega,
                observaciones = p.observaciones,
                fecha = p.fecha,
                total = p.total,
                pagado = p.pagado,      // ← nuevo
                metodo_pago = p.metodo_pago, // ← nuevo
                detalles = p.pedido_detalle.Select(d =>
                {
                    var stock = stockDict.TryGetValue(d.producto_id, out var s) ? s : 0;

                    var semaforo = stock >= d.cantidad
                        ? (stock < d.cantidad * 2 ? "justo" : "ok")
                        : "sin_stock";

                    return new SeguimientoDetalleDTO
                    {
                        producto_id = d.producto_id,
                        producto = d.productos.nombre,
                        cantidad = d.cantidad,
                        precio = d.precio,
                        subtotal = d.subtotal,
                        stock_actual = stock,
                        semaforo = semaforo
                    };
                }).ToList()
            }).ToList();

            return Ok(resultado);
        }


        // GET /api/distribucion
        // Solo pedidos DELIVERY en estado LISTO (4) o DESPACHADO (7)
        [HttpGet("/api/distribucion")]
        public async Task<IActionResult> GetPedidosDistribucion()
        {
            var sucursalId = GetSucursalId();
            var rolId = GetRolId();

            var pedidos = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.sucursal_id == sucursalId
                         && p.tipos_pedido == "DELIVERY"
                         && (p.estado_pedido_id == 4   // LISTO
                          || p.estado_pedido_id == 7)) // DESPACHADO
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
                    p.direccion_entrega,
                    p.metodo_pago,
                    p.pagado,
                    p.estado_pedido_id,
                    estado = p.estado_pedido_id == 4 ? "LISTO" : "DESPACHADO",
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
    }
}