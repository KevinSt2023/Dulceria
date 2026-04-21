using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Application.DTOs
{
    public class CrearPedidoDTOs
    {
        public int cliente_id { get; set; }
        public int usuario_id { get; set; }
        public int sucursal_id { get; set; }
        public string? observaciones { get; set; }
        public string? direccion_entrega {  get; set; }
        public string? tipos_pedido { get; set; }
        public bool pagado { get; set; } = false; 
        public string? metodo_pago { get; set; }
        public int? sucursal_recojo_id { get; set; }
        public List<PedidoDetalleDTOs>? detalles { get; set; }
    }
}
