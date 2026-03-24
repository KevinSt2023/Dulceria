using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class Unidades_Medida
    {
        public int unidad_id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public string abreviatura { get; set; } = string.Empty;
        public bool activo { get; set; }
    }
}
