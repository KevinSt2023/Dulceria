using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class MetodosPago : TenantEntity
    {
        public int metodo_pago_id { get; set; }
        public string nombre { get; set; } = "";
        public string codigo { get; set; } = "";
        public bool activo { get; set; } = true;
    }
}
