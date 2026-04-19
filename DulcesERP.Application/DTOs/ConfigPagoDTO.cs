using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Application.DTOs
{
    public class ConfigPagoDTO
    {
        public string? numero { get; set; }
        public string? titular { get; set; }
        public string? banco { get; set; }
        public string? qr_base64 { get; set; }
        public bool activo { get; set; } = true;
    }
}
