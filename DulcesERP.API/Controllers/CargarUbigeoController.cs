using DulcesERP.Domain.Entities;
using DulcesERP.Infrastructure.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DulcesERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UbigeoController : ControllerBase
    {
        private readonly DulcesERPContext _context;
        private readonly HttpClient _http;

        public UbigeoController(DulcesERPContext context, HttpClient http)
        {
            _context = context;
            _http = http;
        }

        [AllowAnonymous]
        [HttpPost("cargar")]
        public async Task<IActionResult> CargarUbigeos()
        {
            var url = "https://free.e-api.net.pe/ubigeos.json";

            var response = await _http.GetStringAsync(url);
            var json = JsonDocument.Parse(response);

            foreach (var dep in json.RootElement.EnumerateObject())
            {
                var nombre = dep.Name;

                var existe = await _context.Departamentos
                    .AnyAsync(d => d.nombre == nombre);

                if (!existe)
                {
                    _context.Departamentos.Add(new Departamentos
                    {
                        nombre = nombre
                    });
                }
            }

            await _context.SaveChangesAsync();

            foreach (var dep in json.RootElement.EnumerateObject())
            {
                var departamento = await _context.Departamentos
                    .FirstAsync(d => d.nombre == dep.Name);

                foreach (var prov in dep.Value.EnumerateObject())
                {
                    var existe = await _context.Provincias
                        .AnyAsync(p =>
                            p.nombre == prov.Name &&
                            p.departamento_id == departamento.departamento_id);

                    if (!existe)
                    {
                        _context.Provincias.Add(new Provincia
                        {
                            nombre = prov.Name,
                            departamento_id = departamento.departamento_id
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            foreach (var dep in json.RootElement.EnumerateObject())
            {
                var departamento = await _context.Departamentos
                    .FirstAsync(d => d.nombre == dep.Name);

                foreach (var prov in dep.Value.EnumerateObject())
                {
                    var provincia = await _context.Provincias
                        .FirstAsync(p =>
                            p.nombre == prov.Name &&
                            p.departamento_id == departamento.departamento_id);

                    foreach (var dist in prov.Value.EnumerateObject())
                    {
                        var obj = dist.Value;

                        var ubigeo = obj.GetProperty("ubigeo").GetString();

                        var existe = await _context.Distritos
                            .AnyAsync(d => d.ubigeo == ubigeo);

                        if (!existe)
                        {
                            var inei = obj.TryGetProperty("inei", out var ineiProp)
                                ? ineiProp.GetString()
                                : null;

                            _context.Distritos.Add(new Distritos
                            {
                                nombre = dist.Name,
                                provincia_id = provincia.provincia_id,
                                ubigeo = ubigeo,
                                ubigeo_inei = inei
                            });
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Ubigeos cargados correctamente 🚀" });
        }

        [AllowAnonymous]
        [HttpGet("departamentos")]
        public async Task<IActionResult> GetDepartamentos()
        {
            var depa = await _context.Departamentos
                .OrderBy(d => d.nombre)
                .Select(c => new { c.departamento_id, c.nombre })
                .ToListAsync();
            return Ok(depa);
        }

        [AllowAnonymous]
        [HttpGet("provincias/{departamentoId}")]
        public async Task<IActionResult> GetProvincias(int departamentoId)
        {
            var prov = await _context.Provincias
                .Where(p => p.departamento_id == departamentoId)
                .OrderBy(p => p.nombre)
                .Select(c => new { c.provincia_id, c.nombre })
                .ToListAsync();
            return Ok(prov);
        }

        [AllowAnonymous]
        [HttpGet("distritos/{provinciaId}")]
        public async Task<IActionResult> GetDistritos(int provinciaId)
        {
            var dist = await _context.Distritos
                .Where(d => d.provincia_id == provinciaId)
                .OrderBy(d => d.nombre)
                .Select(c => new { c.distrito_id, c.nombre })
                .ToListAsync();
            return Ok(dist);
        }
    }
}
