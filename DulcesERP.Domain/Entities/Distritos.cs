using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class Distritos
    {
        public int distrito_id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string ubigeo { get; set; } = string.Empty;
        public string? ubigeo_inei { get; set; }
        public int provincia_id { get; set; }
        public Provincia? Provincia { get; set; }
    }
}
