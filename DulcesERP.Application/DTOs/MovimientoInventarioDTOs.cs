using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Application.DTOs
{
    public class MovimientoInventarioDTOs
    {
        public int producto_id { get; set; }
        public int almacen_id { get; set; }
        public decimal cantidad { get; set; }
        public string tipo_movimiento { get; set; } = string.Empty;
        public string motivo { get; set; } = string.Empty;
    }
}
