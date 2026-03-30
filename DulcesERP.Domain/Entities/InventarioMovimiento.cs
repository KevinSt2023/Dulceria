using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class InventarioMovimiento
    {
        public int movimiento_id { get; set; }
        public int producto_id { get; set; }
        public int almacen_id { get; set; }
        public string tipo_movimiento { get; set; } = string.Empty;
        public int cantidad { get; set; }
        public int? referencia { get; set; }
        public DateTime fecha { get; set; }
        public int stock_antes { get; set; }
        public int stock_despues { get; set; }
        public string motivo { get; set; } = string.Empty;
    }
}
