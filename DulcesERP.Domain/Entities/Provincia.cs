using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class Provincia
    {
        public int provincia_id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public int departamento_id { get; set; }
        public Departamentos? Departamento { get; set; }
        public ICollection<Distritos> Distritos { get; set; } = new List<Distritos>();
    }
}
