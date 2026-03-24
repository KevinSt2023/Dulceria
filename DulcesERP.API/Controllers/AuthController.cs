using DulcesERP.Application.DTOs;
using DulcesERP.Application.Services;
using DulcesERP.Infrastructure.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context.Usuarios
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.email.Trim().ToLower() == request.email.Trim().ToLower());

            if (user == null)
                return Unauthorized("Usuario no existe");

            if (user.password_hash.Trim() != request.password.Trim())
                return Unauthorized("Password incorrecto");

            var token = _jwtServices.GenerateToken(
                user.usuario_id,
                user.tenant_id,
                user.email
            );

            return Ok(new { token });
        }
    }
}
