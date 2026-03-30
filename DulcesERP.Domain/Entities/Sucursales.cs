using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class Sucursales : TenantEntity
    {
        public int sucursal_id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string direccion { get; set; } = string.Empty;
        public string telefono { get; set; } = string.Empty;
        public bool activo { get; set; }
        public DateTime created_at { get; set; }
    }
}
