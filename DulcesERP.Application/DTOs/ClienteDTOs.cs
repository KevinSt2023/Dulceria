using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Application.DTOs
{
    public class ClienteDTOs
    {
        public int cliente_id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string documento { get; set; } = string.Empty;
        public string telefono { get; set; } = string.Empty;
        public string direccion { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public int? departamento_id { get; set; }
        public int? provincia_id { get; set; }
        public int? distrito_id { get; set; }
        public bool activo { get; set; }
    }
}
