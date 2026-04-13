using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DulcesERP.Application.DTOs;
using DulcesERP.Domain.Entities;
using DulcesERP.Infrastructure.Context;

namespace DulcesERP.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ClienteController : ControllerBase
    {
        private readonly DulcesERPContext _context;
        public ClienteController(DulcesERPContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetClientes()
        {
            var cliente = await _context.Clientes
                .Select(c => new
                {
                    c.cliente_id,
                    c.nombre,
                    c.documento,
                    c.telefono,
                    c.direccion,
                    c.departamento_id,
                    c.provincia_id,
                    c.distrito_id,
                    c.email,
                    c.activo,
                    departamento = c.departamentos.nombre,
                    provincia = c.provincia.nombre,
                    distrito = c.distrito.nombre
                })
                .OrderBy(c => c.cliente_id)
                .ToListAsync();

            if (cliente == null)
                return NotFound();
            
            return Ok(cliente);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetClientes(int id)
        {
            var clientes = await _context.Clientes.FirstOrDefaultAsync(i => i.cliente_id == id);

            if(clientes == null)
                return NotFound();

            await _context.SaveChangesAsync();
            return Ok(clientes);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCliente(ClienteDTOs dto)
        {
            var existe = await _context.Clientes.AnyAsync(c => c.documento == dto.documento);
            if (existe)
                return BadRequest("El cliente ya se encuentra registrado");

            var cliente = new Clientes
            {
                cliente_id = dto.cliente_id,
                nombre = dto.nombre,
                documento = dto.documento,
                telefono = dto.telefono,
                direccion = dto.direccion,
                email = dto.email,
                activo = true,
                created_at = DateTime.UtcNow,
                departamento_id = dto.departamento_id,
                provincia_id = dto.provincia_id,
                distrito_id = dto.distrito_id
            };

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
            return Ok(cliente);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCliente(int id, [FromBody] ClienteDTOs dto)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();

            cliente.cliente_id = dto.cliente_id;
            cliente.nombre = dto.nombre;
            cliente.documento = dto.documento;
            cliente.telefono = dto.telefono;
            cliente.direccion = dto.direccion;
            cliente.email = dto.email;
            cliente.activo = dto.activo;
            cliente.departamento_id = dto.departamento_id;
            cliente.provincia_id = dto.provincia_id;
            cliente.distrito_id = dto.distrito_id;

            await _context.SaveChangesAsync();
            return Ok(cliente);
        }

        [HttpGet("buscar")]
        public async Task<IActionResult> SearchCliente(string documento)
        {
            var cliente = await _context.Clientes
        .Where(c => c.documento == documento)
        .Select(c => new
        {
            c.cliente_id,
            c.nombre,
            c.documento,
            c.telefono,
            c.direccion,
            c.email
        })
        .FirstOrDefaultAsync();

            if (cliente == null)
                return NotFound();

            return Ok(cliente);
        }
    }
}
