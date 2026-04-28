using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class Abono : TenantEntity
    {
        public int abono_id { get; set; }
        public int pedido_id { get; set; }
        public decimal monto { get; set; }
        public string? metodo_pago { get; set; }
        public string? observacion { get; set; }
        public int? usuario_id { get; set; }
        public DateTime fecha { get; set; }

        // Navegación
        public Pedidos? pedidos { get; set; }
    }
}
