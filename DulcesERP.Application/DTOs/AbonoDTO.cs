using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Application.DTOs
{
    public class AbonoDTO
    {
        public decimal monto { get; set; }
        public string metodo_pago { get; set; } = "EFECTIVO";
        public string? observacion { get; set; }
    }
}
