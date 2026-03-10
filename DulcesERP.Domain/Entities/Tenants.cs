using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class Tenants
    {
        public int tenant_id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string ruc { get; set; } = string.Empty;
        public string direccion { get; set; } = string.Empty;
        public string telefono { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public bool activo { get; set; }
        public DateTime create_ad { get; set; }
    }
}
