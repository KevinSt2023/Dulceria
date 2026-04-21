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
    [Route("api/configuracion-negocio")]
    public class ConfiguracionNegocioController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public ConfiguracionNegocioController(DulcesERPContext context)
        {
            _context = context;
        }

        private int GetRolId() =>
            int.Parse(User.FindFirstValue("rol_id")!);

        // ─────────────────────────────────────────────
        // GET /api/configuracion-negocio
        // Todos los roles pueden leer (para el PDF)
        // ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetConfig()
        {
            var config = await _context.ConfiguracionNegocio
                .AsNoTracking()
                .Select(c => new
                {
                    c.config_id,
                    c.razon_social,
                    c.nombre_comercial,
                    c.ruc,
                    c.direccion,
                    c.telefono,
                    c.email,
                    c.moneda,
                    c.simbolo,
                    c.igv_porcentaje,
                    c.pie_comprobante,
                    tiene_logo = c.logo_base64 != null && c.logo_base64 != ""
                })
                .FirstOrDefaultAsync();

            if (config == null)
                return NotFound("Configuración no encontrada");

            return Ok(config);
        }

        // ─────────────────────────────────────────────
        // GET /api/configuracion-negocio/logo
        // Separado para no cargar el base64 siempre
        // ─────────────────────────────────────────────
        [HttpGet("logo")]
        public async Task<IActionResult> GetLogo()
        {
            var config = await _context.ConfiguracionNegocio
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (config == null || string.IsNullOrEmpty(config.logo_base64))
                return NotFound("Logo no configurado");

            return Ok(new { config.logo_base64 });
        }

        // ─────────────────────────────────────────────
        // PUT /api/configuracion-negocio
        // Solo SuperAdmin y Admin
        // ─────────────────────────────────────────────
        [HttpPut]
        public async Task<IActionResult> UpdateConfig(
            [FromBody] ConfigNegocioDTO dto)
        {
            var rolId = GetRolId();
            if (rolId != 0) return Forbid();

            var config = await _context.ConfiguracionNegocio
                .FirstOrDefaultAsync();

            if (config == null)
            {
                config = new ConfiguracionNegocio();
                _context.ConfiguracionNegocio.Add(config);
            }

            config.razon_social = dto.razon_social;
            config.nombre_comercial = dto.nombre_comercial;
            config.ruc = dto.ruc;
            config.direccion = dto.direccion;
            config.telefono = dto.telefono;
            config.email = dto.email;
            config.moneda = dto.moneda ?? "PEN";
            config.simbolo = dto.simbolo ?? "S/";
            config.pie_comprobante = dto.pie_comprobante;

            if (!string.IsNullOrEmpty(dto.logo_base64))
                config.logo_base64 = dto.logo_base64;

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Configuración guardada" });
        }
    }    
}