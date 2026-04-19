using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class ConfiguracionPago : TenantEntity
    {
        public int config_id { get; set; }
        public string tipo { get; set; } = "";  // yape, plin, transferencia
        public string? numero { get; set; }
        public string? titular { get; set; }
        public string? banco { get; set; }
        public string? qr_base64 { get; set; }        // imagen en base64
        public bool activo { get; set; } = true;
    }
}
