using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class Pagos : TenantEntity
    {
        public int pago_id { get; set; }
        public int venta_id { get; set; }
        public int metodo_pago_id { get; set; }
        public decimal monto { get; set; }
        public string? referencia { get; set; }
        public DateTime fecha { get; set; } = DateTime.UtcNow;

        // Navegación
        public Ventas ventas { get; set; } = null!;
        public MetodosPago metodos_pago { get; set; } = null!;
    }
}
