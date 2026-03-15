using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public abstract class TenantEntity
    {
        public int tenant_id { get; set; }
    }
}
