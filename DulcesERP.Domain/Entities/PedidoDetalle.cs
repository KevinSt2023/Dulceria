using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class PedidoDetalle
    {
        public int detalle_id { get; set; }
        public int pedido_id { get; set; }
        public int producto_id { get; set; }
        public int cantidad { get; set; }
        public decimal precio { get; set; }
        public decimal subtotal { get; set; }
        public decimal? descuento { get; set; }
        public Pedidos pedidos { get; set; } = null!;
        public Productos productos { get; set; } = null!;
    }
}
