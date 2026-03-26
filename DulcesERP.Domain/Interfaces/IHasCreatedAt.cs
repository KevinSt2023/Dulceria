using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Interfaces
{
    public interface IHasCreatedAt
    {
        DateTime created_at { get; set; }
    }
}
