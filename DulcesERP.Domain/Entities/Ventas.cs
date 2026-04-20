using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class Ventas : TenantEntity
    {
        public int venta_id { get; set; }
        public int? pedido_id { get; set; }
        public int cliente_id { get; set; }
        public int usuario_id { get; set; }
        public int? impuesto_id { get; set; }
        public decimal total { get; set; }
        public DateTime fecha { get; set; } = DateTime.UtcNow;

        // Navegación
        public Clientes clientes { get; set; } = null!;
        public Usuarios usuarios { get; set; } = null!;
        public Impuestos? impuestos { get; set; }
        public Pedidos? pedidos { get; set; }
        public List<Comprobantes> comprobantes { get; set; } = new();
        public List<Pagos> pagos { get; set; } = new();
    }
}
