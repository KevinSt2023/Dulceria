using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class Comprobantes : TenantEntity
    {
        public int comprobante_id { get; set; }
        public int venta_id { get; set; }
        public int serie_id { get; set; }
        public int numero { get; set; }
        public int tipo_comprobante_id { get; set; }
        public int cliente_id { get; set; }
        public int? impuesto_id { get; set; }
        public decimal subtotal { get; set; }
        public decimal igv { get; set; }
        public decimal total { get; set; }
        public string estado_sunat { get; set; } = "SIN_ENVIAR";
        public string? hash_cpe { get; set; }
        public string? xml { get; set; }
        public string? cdr { get; set; }
        public DateTime fecha { get; set; } = DateTime.UtcNow;

        // Navegación
        public Ventas ventas { get; set; } = null!;
        public SeriesComprobante series { get; set; } = null!;
        public TiposComprobante tipos_comprobante { get; set; } = null!;
        public Clientes clientes { get; set; } = null!;
        public Impuestos? impuestos { get; set; }
        public List<ComprobanteDetalle> detalles { get; set; } = new();
    }
}
