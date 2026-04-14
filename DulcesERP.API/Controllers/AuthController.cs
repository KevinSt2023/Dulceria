//using DulcesERP.Application.DTOs;
//using DulcesERP.Application.Services;
//using DulcesERP.Infrastructure.Context;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;

//namespace DulcesERP.API.Controllers
//{
//    [ApiController]
//    [Route("api/auth")]
//    public class AuthController : ControllerBase
//    {       

//        private readonly DulcesERPContext _context;
//        private readonly JwtServices _jwtServices;

//        public AuthController(DulcesERPContext context, JwtServices jwtServices)
//        {
//            _context = context;
//            _jwtServices = jwtServices;
//        }

//        [AllowAnonymous]
//        [HttpPost("login")]
//        public async Task<IActionResult> Login(LoginRequest request)
//        {
//            var user = await _context.Usuarios
//                .IgnoreQueryFilters()
//                .FirstOrDefaultAsync(u => u.email.Trim().ToLower() == request.email.Trim().ToLower());

//            if (user == null)
//                return Unauthorized("Usuario no existe");

//            if (!BCrypt.Net.BCrypt.Verify(request.password, user.password_hash))
//                return Unauthorized("Password incorrecto");

//            var token = _jwtServices.GenerateToken(
//                user.usuario_id,
//                user.tenant_id,
//                user.email
//            );

//            return Ok(new { token });
//        }
//    }
//}

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

            if (!BCrypt.Net.BCrypt.Verify(request.password, user.password_hash))
                return Unauthorized("Password incorrecto");

            var token = _jwtServices.GenerateToken(
                user.usuario_id,
                user.tenant_id,
                user.sucursal_id,
                user.rol_id,      // ← nuevo
                user.email
            );

            return Ok(new { token });
        }
    }
}
