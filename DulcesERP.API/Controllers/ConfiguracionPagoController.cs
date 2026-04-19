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
    public class ConfiguracionPagoController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public ConfiguracionPagoController(DulcesERPContext context)
        {
            _context = context;
        }

        private int GetRolId() =>
            int.Parse(User.FindFirstValue("rol_id")!);

        // ─────────────────────────────────────────────
        // GET /api/configuracionpago
        // Devuelve la config de pago del tenant
        // Todos los roles pueden leerla (para mostrar QR)
        // ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetConfig()
        {
            var config = await _context.ConfiguracionPagos
                .AsNoTracking()
                .Where(c => c.activo == true)
                .Select(c => new
                {
                    c.config_id,
                    c.tipo,
                    c.numero,
                    c.titular,
                    c.banco,
                    c.activo,
                    tiene_qr = c.qr_base64 != null && c.qr_base64 != ""
                })
                .ToListAsync();

            return Ok(config);
        }

        // ─────────────────────────────────────────────
        // GET /api/configuracionpago/{tipo}/qr
        // Devuelve el QR en base64 — separado para no
        // cargar el base64 en cada llamada al listado
        // ─────────────────────────────────────────────
        [HttpGet("{tipo}/qr")]
        public async Task<IActionResult> GetQR(string tipo)
        {
            var config = await _context.ConfiguracionPagos
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.tipo == tipo.ToLower() &&
                    c.activo == true);

            if (config == null)
                return NotFound("Configuración no encontrada");

            if (string.IsNullOrEmpty(config.qr_base64))
                return NotFound("QR no configurado");

            return Ok(new
            {
                config.tipo,
                config.numero,
                config.titular,
                config.qr_base64
            });
        }

        // ─────────────────────────────────────────────
        // PUT /api/configuracionpago/{tipo}
        // Solo SuperAdmin y Admin actualizan
        // ─────────────────────────────────────────────
        [HttpPut("{tipo}")]
        public async Task<IActionResult> UpdateConfig(string tipo, [FromBody] ConfigPagoDTO dto)
        {
            var rolId = GetRolId();

            if (rolId != 0 && rolId != 1)
                return Forbid();

            var config = await _context.ConfiguracionPagos
                .FirstOrDefaultAsync(c => c.tipo == tipo.ToLower());

            if (config == null)
            {
                config = new ConfiguracionPago
                {
                    tipo = tipo.ToLower(),
                    activo = true
                };
                _context.ConfiguracionPagos.Add(config);
            }

            config.numero = dto.numero;
            config.titular = dto.titular;
            config.banco = dto.banco;
            config.activo = dto.activo;

            // Solo actualizar QR si viene en el request
            if (!string.IsNullOrEmpty(dto.qr_base64))
                config.qr_base64 = dto.qr_base64;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Configuración actualizada",
                config.tipo,
                config.numero,
                config.titular,
                tiene_qr = !string.IsNullOrEmpty(config.qr_base64)
            });
        }        
    }    
}