using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Application.DTOs
{
    public class EntregarPedidoDTO
    {
        public decimal monto_cobrado { get; set; }
        public string metodo_pago { get; set; } = "";
    }
}
