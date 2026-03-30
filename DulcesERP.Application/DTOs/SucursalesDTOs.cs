using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Application.DTOs
{
    public class SucursalesDTOs
    {
        public string nombre { get; set; } = string.Empty;
        public string direccion { get; set; } = string.Empty;
        public string telefono { get; set; } = string.Empty;
        public bool activo { get; set; }
    }
}
