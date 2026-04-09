using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class Departamentos
    {
        public int departamento_id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public ICollection<Provincia> Provincias { get; set; } = new List<Provincia>();
    }
}
