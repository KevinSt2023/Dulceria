using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class Almacenes : TenantEntity
    {
        public int almacen_id {  get; set; }
        public int sucursal_id { get; set; }
        public string nombre { get; set; } = string.Empty;
        public bool activo { get; set; }
        public Sucursales sucursales { get; set; } = null!; 
    }
}
