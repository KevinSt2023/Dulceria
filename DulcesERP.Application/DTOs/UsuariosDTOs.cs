using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Application.DTOs
{
    public class UsuariosDTOs
    {
        public string nombre { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;        
        public bool activo { get; set; }      
        public int rol_id { get; set; }
    }
}
