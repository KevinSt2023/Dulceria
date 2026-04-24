using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Application.DTOs
{
    public class CambiarPlanDTO
    {
        public int plan_id { get; set; }
        public DateOnly? fecha_inicio { get; set; }
        public DateOnly? fecha_vencimiento { get; set; }
    }
}
