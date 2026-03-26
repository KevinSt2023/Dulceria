using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DulcesERP.Domain.Interfaces;

namespace DulcesERP.Domain.Entities
{
    public class Usuarios : TenantEntity, IHasCreatedAt
    {
        public int usuario_id { get; set; }        
        public string nombre { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string password_hash { get; set; } = string.Empty;
        public bool activo { get; set; }
        public DateTime created_at { get; set; }
        public int rol_id { get; set; }
        public Roles roles { get; set; } = null!;   
    }
}
