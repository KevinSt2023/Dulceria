using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class Inventario : TenantEntity
    {        
        public int inventario_id {  get; set; }
        public int producto_id { get; set; }
        public int almacen_id { get; set; }
        public Almacenes almacenes { get; set; } = null!;
        public Productos productos { get; set; } = null!;
        public int stock_actual { get; set; }
        public int stock_minimo { get; set; }
        public int stock_maximo { get; set; }
        public DateTime updated_at { get; set; }        
    }
}
