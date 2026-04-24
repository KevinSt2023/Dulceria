using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class Plan
    {
        public int plan_id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public int max_sucursales { get; set; }
        public int max_usuarios { get; set; }
        public bool tiene_facturacion_electronica { get; set; }
        public decimal precio_mensual { get; set; }
        public bool activo { get; set; }
        public DateTime created_at { get; set; }

        // Navegación
        public ICollection<Tenants> Tenants { get; set; } = new List<Tenants>();
    }
}
