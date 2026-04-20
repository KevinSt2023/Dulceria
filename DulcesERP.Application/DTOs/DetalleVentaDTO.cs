using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Application.DTOs
{
    public class DetalleVentaDTO
    {
        public int producto_id { get; set; }
        public decimal cantidad { get; set; }
        public decimal precio_unitario { get; set; }
    }
}
