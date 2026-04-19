using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class ProductoSucursal : TenantEntity
    {
        public int producto_id { get; set; }
        public int sucursal_id { get; set; }
        public bool permite_pedido_sin_stock { get; set; } = true;
        public bool activo { get; set; } = true;

        public Productos productos { get; set; } = null!;
        public Sucursales sucursales { get; set; } = null!;
    }
}
