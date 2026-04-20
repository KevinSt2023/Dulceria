using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class TiposComprobante
    {
        public int tipo_comprobante_id { get; set; }
        public string codigo_sunat { get; set; } = "";
        public string nombre { get; set; } = "";
    }
}
