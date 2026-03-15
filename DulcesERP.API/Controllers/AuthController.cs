using Microsoft.AspNetCore.Mvc;
using DulcesERP.Application.Services;
using DulcesERP.Application.DTOs;
using DulcesERP.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace DulcesERP.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        //private readonly AuthServices _authServices;

        //public AuthController(AuthServices authServices)
        //{
        //    _authServices = authServices;
        //}

        //[HttpPost("login")]
        //public async Task<IActionResult> Login(LoginRequest request)
        //{
        //    var validUser = await _authServices.ValidateUser(request.email, request.password);

        //    if (!validUser)
        //        return Unauthorized("Credenciales no autorizadas");

        //    return Ok("Login correcto");
        //}

        private readonly DulcesERPContext _context;
        private readonly JwtServices _jwtServices;

        public AuthController(DulcesERPContext context, JwtServices jwtServices)
        {
            _context = context;
            _jwtServices = jwtServices;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.email == request.email && 
            u.password_hash == request.password);

            if(user == null)
                return Unauthorized("Credenciales no autorizadas");

            var token = _jwtServices.GenerateToken(
                user.usuario_id,
                user.tenant_id,
                user.email
                );

            return Ok(new { token });
        }
    }
}
