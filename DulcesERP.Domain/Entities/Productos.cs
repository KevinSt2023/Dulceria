using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DulcesERP.Domain.Interfaces;

namespace DulcesERP.Domain.Entities
{
    public class Productos : TenantEntity, IHasCreatedAt
    {
        public int producto_id { get; set; }
        public int categoria_id { get; set; }
        public Categorias categorias { get; set; } = null!;
        public int unidad_id { get; set; }
        public Unidades_Medida unidades { get; set; } = null!;
        public int tipo_producto_id { get; set; }
        public Tipos_Productos tipos { get; set; } = null!;
        public string nombre { get; set; } = string.Empty;
        public string descripcion { get; set; } = string.Empty;
        public decimal precio { get; set; }
        public decimal costo { get; set; }
        public bool activo { get; set; }
        public DateTime created_at { get; set; }
    }
}
