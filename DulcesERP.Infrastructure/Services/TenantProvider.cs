using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace DulcesERP.Infrastructure.Services
{
    public class TenantProvider
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public TenantProvider(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public int GetTenantId()
        {
            var tenantClaim = _contextAccessor.HttpContext?.User.FindFirst("tenant_id");

            if(tenantClaim == null)
                return 0;

            return int.Parse(tenantClaim.Value);
        }
    }
}
