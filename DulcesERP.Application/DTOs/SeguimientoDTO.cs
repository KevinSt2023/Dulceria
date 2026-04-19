using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Application.DTOs
{
    public class SeguimientoDTO
    {
        public class SeguimientoItemDTO
        {
            public int pedido_id { get; set; }
            public string cliente { get; set; } = "";
            public string estado { get; set; } = "";
            public int estado_id { get; set; }
            public string tipos_pedido { get; set; } = "";
            public string? direccion_entrega { get; set; }
            public string? observaciones { get; set; }
            public DateTime fecha { get; set; }
            public decimal total { get; set; }

            public List<SeguimientoDetalleDTO> detalles { get; set; } = new();
        }

        public class SeguimientoDetalleDTO
        {
            public int producto_id { get; set; }
            public string producto { get; set; } = "";
            public int cantidad { get; set; }
            public decimal precio { get; set; }
            public decimal subtotal { get; set; }
            public int stock_actual { get; set; }
            public string semaforo { get; set; } = ""; // "ok" | "justo" | "sin_stock"
        }
    }
}
