using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Application.DTOs
{
    public class PedidosDTOs
    {
        public int cliente_id { get; set; }
        public int sucursal_id { get; set; }
        public string? observaciones { get; set; }
        public string? direccion_entrega { get; set; }
        public string? tipos_pedido { get; set; }
        public List<PedidoDetalleDTOs> pedido_detalle { get; set; } = new List<PedidoDetalleDTOs>();
    }
}
