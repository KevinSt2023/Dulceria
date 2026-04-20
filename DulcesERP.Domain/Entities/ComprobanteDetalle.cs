using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class ComprobanteDetalle : TenantEntity
    {
        public int detalle_id { get; set; }
        public int comprobante_id { get; set; }
        public int producto_id { get; set; }
        public decimal cantidad { get; set; }
        public decimal precio_unitario { get; set; }
        public decimal subtotal { get; set; }
        public decimal igv { get; set; }
        public decimal total { get; set; }

        // Navegación
        public Comprobantes comprobantes { get; set; } = null!;
        public Productos productos { get; set; } = null!;
    }
}
