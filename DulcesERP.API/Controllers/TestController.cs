using Microsoft.AspNetCore.Mvc;
using DulcesERP.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace DulcesERP.API.Controllers
{
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        private readonly DulcesERPContext _context;

        public TestController(DulcesERPContext context)
        {
            _context = context;
        }

        [HttpGet("usuarios")]
        public async Task<IActionResult> GetUsuarios() 
        {
            var usuarios = await _context.Usuarios.ToListAsync();
            return Ok(usuarios);
        }
    }
}
