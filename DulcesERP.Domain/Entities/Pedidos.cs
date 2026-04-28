using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Domain.Entities
{
    public class Pedidos : TenantEntity
    {
        public int pedido_id { get; set; }
        public int cliente_id { get; set; }
        public int usuario_id { get; set; }
        public int sucursal_id { get; set; }
        public int estado_pedido_id { get; set; }
        public decimal total { get; set; }
        public DateTime fecha { get; set; }
        public string? observaciones { get; set; }
        public string? direccion_entrega { get; set; }
        public string? tipos_pedido { get; set; }
        public bool pagado { get; set; } = false;
        public string? metodo_pago { get; set; }
        public string tipo_pago { get; set; } = "CONTADO";
        public decimal monto_pagado { get; set; } = 0;
        public decimal saldo_pendiente { get; set; } = 0;
        public Clientes clientes { get; set; } = null!;
        public Usuarios usuarios { get; set; } = null!;
        public Sucursales sucursales { get; set; } = null!;
        public EstadosPedido estados_pedidos { get; set; } = null!;
        public ICollection<PedidoDetalle> pedido_detalle { get; set; } = new List<PedidoDetalle>();
    }
}
