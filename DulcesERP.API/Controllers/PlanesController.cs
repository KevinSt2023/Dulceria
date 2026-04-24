using DulcesERP.Domain.Entities;
using DulcesERP.Infrastructure.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DulcesERP.Application.DTOs;

namespace DulcesERP.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PlanesController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public PlanesController(DulcesERPContext context)
        {
            _context = context;
        }

        private int GetRolId() =>
            int.Parse(User.FindFirstValue("rol_id")!);

        // ── GET /api/planes — listar todos los planes ─────────────────────
        [HttpGet]
        public async Task<IActionResult> GetPlanes()
        {
            var planes = await _context.Planes
                .AsNoTracking()
                .Where(p => p.activo)
                .OrderBy(p => p.plan_id)
                .Select(p => new
                {
                    p.plan_id,
                    p.nombre,
                    p.max_sucursales,
                    p.max_usuarios,
                    p.tiene_facturacion_electronica,
                    p.precio_mensual
                })
                .ToListAsync();

            return Ok(planes);
        }

        // ── GET /api/planes/tenants — listar tenants con su plan (SuperAdmin) ──
        [HttpGet("tenants")]
        public async Task<IActionResult> GetTenants()
        {
            if (GetRolId() != 0)
                return Forbid();

            var tenants = await _context.Tenants
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(t => t.plan)
                .OrderBy(t => t.tenant_id)
                .Select(t => new
                {
                    t.tenant_id,
                    t.nombre,
                    t.ruc,
                    t.email,
                    t.activo,
                    plan = new
                    {
                        t.plan.plan_id,
                        t.plan.nombre,
                        t.plan.precio_mensual
                    },
                    t.plan_fecha_inicio,
                    t.plan_fecha_vencimiento,
                    t.plan_activo,
                    // Uso actual
                    total_usuarios = _context.Usuarios
                        .Count(u => u.tenant_id == t.tenant_id && u.activo),
                    total_sucursales = _context.Sucursales
                        .Count(s => s.tenant_id == t.tenant_id && s.activo)
                })
                .ToListAsync();

            return Ok(tenants);
        }

        // ── PUT /api/planes/tenants/{id}/plan — cambiar plan (SuperAdmin) ──
        [HttpPut("tenants/{tenantId}/plan")]
        public async Task<IActionResult> CambiarPlan(int tenantId, [FromBody] CambiarPlanDTO dto)
        {
            if (GetRolId() != 0)
                return Forbid();

            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.tenant_id == tenantId);

            if (tenant == null)
                return NotFound("Tenant no encontrado");

            var plan = await _context.Planes
                .FirstOrDefaultAsync(p => p.plan_id == dto.plan_id && p.activo);

            if (plan == null)
                return BadRequest("Plan no válido");

            tenant.plan_id = dto.plan_id;
            tenant.plan_fecha_inicio = dto.fecha_inicio ?? DateOnly.FromDateTime(DateTime.Today);
            tenant.plan_fecha_vencimiento = dto.fecha_vencimiento ??
                DateOnly.FromDateTime(DateTime.Today.AddDays(30));
            tenant.plan_activo = true;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = $"Plan actualizado a {plan.nombre}",
                tenant = tenant.nombre,
                plan = plan.nombre,
                vence = tenant.plan_fecha_vencimiento
            });
        }

        // ── GET /api/planes/mi-plan — info del plan del tenant actual ─────
        [HttpGet("mi-plan")]
        public async Task<IActionResult> MiPlan()
        {
            var tenantId = int.Parse(User.FindFirstValue("tenant_id")!);

            var tenant = await _context.Tenants
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(t => t.plan)
                .FirstOrDefaultAsync(t => t.tenant_id == tenantId);

            if (tenant == null)
                return NotFound();

            var totalUsuarios = await _context.Usuarios
                .CountAsync(u => u.tenant_id == tenantId && u.activo);
            var totalSucursales = await _context.Sucursales
                .CountAsync(s => s.tenant_id == tenantId && s.activo);

            return Ok(new
            {
                plan = tenant.plan.nombre,
                precio = tenant.plan.precio_mensual,
                plan_fecha_vencimiento = tenant.plan_fecha_vencimiento,
                plan_activo = tenant.plan_activo,
                tiene_facturacion = tenant.plan.tiene_facturacion_electronica,
                limites = new
                {
                    max_usuarios = tenant.plan.max_usuarios,
                    max_sucursales = tenant.plan.max_sucursales,
                    usuarios_usados = totalUsuarios,
                    sucursales_usadas = totalSucursales,
                    usuarios_disponibles = tenant.plan.max_usuarios == 0 ? 999 :
                        tenant.plan.max_usuarios - totalUsuarios,
                    sucursales_disponibles = tenant.plan.max_sucursales == 0 ? 999 :
                        tenant.plan.max_sucursales - totalSucursales
                }
            });
        }
    }    
}