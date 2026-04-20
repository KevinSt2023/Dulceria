using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class Impuestos : TenantEntity
    {
        public int impuesto_id { get; set; }
        public string nombre { get; set; } = "";
        public decimal porcentaje { get; set; }
    }
}
