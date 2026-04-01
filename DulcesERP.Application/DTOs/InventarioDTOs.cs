using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Application.DTOs
{
    public class InventarioDTOs
    {
        public int producto_id {  get; set; }
        public int almacen_id { get; set; }
        public int stock_minimo { get; set; }
        public int stock_maximo { get; set; }
        public DateTime updated_at { get; set; }
    }
}
