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
    public class DashboardController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public DashboardController(DulcesERPContext context)
        {
            _context = context;
        }

        private int GetRolId() => int.Parse(User.FindFirstValue("rol_id")!);
        private int GetSucursalId() => int.Parse(User.FindFirstValue("sucursal_id")!);

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var rolId = GetRolId();
            var sucursalId = GetSucursalId();
            var hoy = DateTime.UtcNow.Date;

            // ── Pedidos ──────────────────────────────
            var queryPedidos = _context.Pedidos.AsQueryable();
            if (rolId != 0)
                queryPedidos = queryPedidos
                    .Where(p => p.sucursal_id == sucursalId);

            var pedidosHoy = await queryPedidos
                .Where(p => p.fecha >= hoy)
                .CountAsync();

            var pedidosPendientes = await queryPedidos
                .Where(p => p.estado_pedido_id == 1)
                .CountAsync();

            var pedidosEnProduccion = await queryPedidos
                .Where(p => p.estado_pedido_id == 3)
                .CountAsync();

            var pedidosListos = await queryPedidos
                .Where(p => p.estado_pedido_id == 4)
                .CountAsync();

            var ventasHoy = await queryPedidos
                .Where(p => p.fecha >= hoy && p.estado_pedido_id == 5)
                .SumAsync(p => (decimal?)p.total) ?? 0;

            var ventasMes = await queryPedidos
                .Where(p => p.fecha >= new DateTime(hoy.Year, hoy.Month, 1)
                         && p.estado_pedido_id == 5)
                .SumAsync(p => (decimal?)p.total) ?? 0;

            // ── Inventario ───────────────────────────
            var queryInv = _context.Inventario.AsQueryable();
            if (rolId != 0)
                queryInv = queryInv
                    .Where(i => i.almacenes.sucursal_id == sucursalId);

            var stockBajo = await queryInv
                .Where(i => i.stock_actual <= i.stock_minimo && i.stock_minimo > 0)
                .Select(i => new
                {
                    producto = i.productos.nombre,
                    almacen = i.almacenes.nombre,
                    stock = (int)i.stock_actual,
                    minimo = i.stock_minimo
                })
                .ToListAsync();

            var totalProductos = await _context.Productos
                .CountAsync(p => p.activo == true);

            // ── Usuarios ─────────────────────────────
            var queryUsuarios = _context.Usuarios.AsQueryable();
            if (rolId != 0)
                queryUsuarios = queryUsuarios
                    .Where(u => u.sucursal_id == sucursalId);

            var totalUsuarios = await queryUsuarios
                .CountAsync(u => u.activo == true);

            // ── Pedidos recientes ────────────────────
            var pedidosRecientes = await queryPedidos
                .OrderByDescending(p => p.fecha)
                .Take(5)
                .Select(p => new
                {
                    p.pedido_id,
                    cliente = p.clientes.nombre,
                    estado = p.estados_pedidos.nombre,
                    total = p.total,
                    fecha = p.fecha,
                    sucursal = p.sucursales.nombre
                })
                .ToListAsync();

            return Ok(new
            {
                // Pedidos
                pedidos_hoy = pedidosHoy,
                pedidos_pendientes = pedidosPendientes,
                pedidos_en_produccion = pedidosEnProduccion,
                pedidos_listos = pedidosListos,

                // Ventas
                ventas_hoy = ventasHoy,
                ventas_mes = ventasMes,

                // Inventario
                stock_bajo = stockBajo,
                total_productos = totalProductos,

                // Usuarios
                total_usuarios = totalUsuarios,

                // Recientes
                pedidos_recientes = pedidosRecientes
            });
        }
    }
}