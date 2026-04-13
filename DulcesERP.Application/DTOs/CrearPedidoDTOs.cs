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
        public List<PedidoDetalleDTOs>? detalles { get; set; }
    }
}
