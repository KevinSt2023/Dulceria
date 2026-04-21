using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Application.DTOs
{
    public class CrearVentaDTO
    {
        public int? pedido_id { get; set; } // null si es venta directa
        public int? cliente_id { get; set; } // null si el cliente es generico
        public int tipo_comprobante_id { get; set; } // 1=Factura, 2=Boleta
        public int metodo_pago_id { get; set; }
        public int impuesto_id { get; set; } = 1; // IGV por defecto
        public decimal monto_pagado { get; set; }
        public string? referencia_pago { get; set; }
        public List<DetalleVentaDTO> detalles { get; set; } = new();
    }
}
