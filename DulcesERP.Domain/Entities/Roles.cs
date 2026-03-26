using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class Roles
    {
        public int rol_id { get; set; }        
        public string nombre { get; set; } = string.Empty;   
        public bool activo  { get; set; }
    }
}
