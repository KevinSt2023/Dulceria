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

            if (rolId != 0 && rolId != 1 && rolId != 3)
                return Forbid();

            var estadosVisibles = new[] { 1, 2, 3 };

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
    }
}