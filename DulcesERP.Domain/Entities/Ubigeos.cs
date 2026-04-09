using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class Ubigeos
    {
        public int ubigeo_id { get; set; }
        public string departamento { get; set; } = string.Empty;
        public string provincia {  get; set; } = string.Empty;
        public string distrito {  get; set; } = string.Empty;
        public string ubigeo {  get; set; } = string.Empty;
        public string ubigeo_inei {  get; set; } = string.Empty;
    }
}
