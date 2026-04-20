using DulcesERP.Application.DTOs;
using DulcesERP.Domain.Entities;
using DulcesERP.Infrastructure.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DulcesERP.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ClienteController : ControllerBase
    {
        private readonly DulcesERPContext _context;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public ClienteController(
            DulcesERPContext context,
            IConfiguration config,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _config = config;
            _httpClientFactory = httpClientFactory;
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

            return Ok(cliente);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCliente(int id)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(i => i.cliente_id == id);

            if (cliente == null) return NotFound();
            return Ok(cliente);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCliente(ClienteDTOs dto)
        {
            var existe = await _context.Clientes
                .AnyAsync(c => c.documento == dto.documento);

            if (existe)
                return BadRequest("El cliente ya se encuentra registrado");

            var cliente = new Clientes
            {
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
            return Ok(new
            {
                cliente.cliente_id,
                cliente.nombre,
                cliente.documento
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCliente(int id, [FromBody] ClienteDTOs dto)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null) return NotFound();

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
                    c.email,
                    c.departamento_id,
                    c.provincia_id,
                    c.distrito_id,
                    departamento_nombre = c.departamentos != null
                        ? c.departamentos.nombre : null,
                    provincia_nombre = c.provincia != null
                        ? c.provincia.nombre : null,
                    distrito_nombre = c.distrito != null
                        ? c.distrito.nombre : null
                })
                .FirstOrDefaultAsync();

            if (cliente == null) return NotFound();
            return Ok(cliente);
        }

        // ─────────────────────────────────────────────
        // GET /api/cliente/consultar-dni/{dni}
        // ─────────────────────────────────────────────
        [HttpGet("consultar-dni/{dni}")]
        public async Task<IActionResult> ConsultarDNI(string dni)
        {
            if (dni.Length != 8)
                return BadRequest("DNI debe tener 8 dígitos");

            // 1. Buscar en BD primero
            var enBD = await _context.Clientes
                .FirstOrDefaultAsync(c => c.documento == dni);

            if (enBD != null)
                return Ok(new
                {
                    encontrado_en_bd = true,
                    enBD.cliente_id,
                    enBD.nombre,
                    enBD.documento,
                    enBD.telefono,
                    enBD.email
                });

            // 2. Consultar RENIEC via Decolecta
            try
            {
                var token = _config["ApisNet:Token"];
                var http = _httpClientFactory.CreateClient();
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var response = await http.GetAsync(
                    $"https://api.decolecta.com/v1/reniec/dni?numero={dni}"
                );

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"DNI status: {response.StatusCode} | json: {json}");

                if (!response.IsSuccessStatusCode)
                    return Ok(new
                    {
                        encontrado_en_bd = false,
                        nombre = "",
                        documento = dni,
                        error = json
                    });

                var opts = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var data = System.Text.Json.JsonSerializer
                    .Deserialize<DecolectaDNIResponse>(json, opts);

                string nombre = data?.full_name ??
                    $"{data?.first_last_name} {data?.second_last_name} {data?.first_name}"
                    .Trim();

                return Ok(new
                {
                    encontrado_en_bd = false,
                    cliente_id = (int?)null,
                    nombre,
                    documento = dni
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error DNI: {ex.Message}");
                return StatusCode(500, "Error al consultar RENIEC");
            }
        }

        // ─────────────────────────────────────────────
        // GET /api/cliente/consultar-ruc/{ruc}
        // ─────────────────────────────────────────────
        [HttpGet("consultar-ruc/{ruc}")]
        public async Task<IActionResult> ConsultarRUC(string ruc)
        {
            if (ruc.Length != 11)
                return BadRequest("RUC debe tener 11 dígitos");

            // 1. Buscar en BD primero
            var enBD = await _context.Clientes
                .FirstOrDefaultAsync(c => c.documento == ruc);

            if (enBD != null)
                return Ok(new
                {
                    encontrado_en_bd = true,
                    enBD.cliente_id,
                    enBD.nombre,
                    enBD.documento
                });

            // 2. Consultar SUNAT via Decolecta
            try
            {
                var token = _config["ApisNet:Token"];
                var http = _httpClientFactory.CreateClient();
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var response = await http.GetAsync(
                    $"https://api.decolecta.com/v1/sunat/ruc?numero={ruc}"
                );

                var json = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"RUC status: {response.StatusCode} | json: {json}");

                if (!response.IsSuccessStatusCode)
                    return Ok(new
                    {
                        encontrado_en_bd = false,
                        nombre = "",
                        documento = ruc,
                        error = json
                    });

                var opts = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var data = System.Text.Json.JsonSerializer
                    .Deserialize<DecolectaRUCResponse>(json, opts);

                return Ok(new
                {
                    encontrado_en_bd = false,
                    cliente_id = (int?)null,
                    nombre = data?.razon_social ?? "",
                    documento = ruc,
                    direccion = data?.direccion ?? "",
                    distrito = data?.distrito ?? "",
                    provincia = data?.provincia ?? "",
                    departamento = data?.departamento ?? "",
                    estado = data?.estado ?? "",
                    condicion = data?.condicion ?? ""
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error RUC: {ex.Message}");
                return StatusCode(500, "Error al consultar SUNAT");
            }
        }

        // ── DTOs Decolecta ──
        public class DecolectaDNIResponse
        {
            public string? first_name { get; set; }
            public string? first_last_name { get; set; }
            public string? second_last_name { get; set; }
            public string? full_name { get; set; }
            public string? document_number { get; set; }
        }

        public class DecolectaRUCResponse
        {
            public string? razon_social { get; set; }
            public string? numero_documento { get; set; }
            public string? estado { get; set; }
            public string? condicion { get; set; }
            public string? direccion { get; set; }
            public string? distrito { get; set; }
            public string? provincia { get; set; }
            public string? departamento { get; set; }
        }
    }
}