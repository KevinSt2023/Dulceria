using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class ConfiguracionNegocio : TenantEntity
    {
        public int config_id { get; set; }
        public string razon_social { get; set; } = "";
        public string? nombre_comercial { get; set; }
        public string? ruc { get; set; }
        public string? direccion { get; set; }
        public string? telefono { get; set; }
        public string? email { get; set; }
        public string? logo_base64 { get; set; }
        public string moneda { get; set; } = "PEN";
        public string simbolo { get; set; } = "S/";
        public decimal igv_porcentaje { get; set; } = 18;
        public string? pie_comprobante { get; set; }
        public bool activo { get; set; } = true;
    }
}
