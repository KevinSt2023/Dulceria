using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class EstadosPedido
    {
        public int estado_pedido_id { get; set; }
        public string nombre { get; set; } = null!;
        public ICollection<Pedidos> pedidos { get; set; } = new List<Pedidos>();    
    }
}
