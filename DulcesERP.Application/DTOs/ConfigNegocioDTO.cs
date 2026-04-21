using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Application.DTOs
{
    public class ConfigNegocioDTO
    {
        public string razon_social { get; set; } = "";
        public string? nombre_comercial { get; set; }
        public string? ruc { get; set; }
        public string? direccion { get; set; }
        public string? telefono { get; set; }
        public string? email { get; set; }
        public string? logo_base64 { get; set; }
        public string? moneda { get; set; }
        public string? simbolo { get; set; }
        public string? pie_comprobante { get; set; }
    }
}
