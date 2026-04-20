using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class SeriesComprobante : TenantEntity
    {
        public int serie_id { get; set; }
        public int sucursal_id { get; set; }
        public int tipo_comprobante_id { get; set; }
        public string serie { get; set; } = "";
        public int correlativo_actual { get; set; } = 0;
        public bool activo { get; set; } = true;

        // Navegación
        public Sucursales sucursales { get; set; } = null!;
        public TiposComprobante tipos_comprobante { get; set; } = null!;
    }
}
