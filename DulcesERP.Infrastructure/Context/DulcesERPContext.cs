using DulcesERP.Domain.Entities;
using DulcesERP.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Infrastructure.Context
{
    public class DulcesERPContext : DbContext
    {
        private readonly TenantProvider _tenantProvider;
        public DulcesERPContext(DbContextOptions<DulcesERPContext> options, TenantProvider tenantProvider) : base(options)
        {
            _tenantProvider = tenantProvider;
        }

        public DbSet<ProductoSucursal> ProductoSucursales { get; set; }
        public DbSet<Tenants> Tenants { get; set; }
        public DbSet<ConfiguracionNegocio> ConfiguracionNegocio { get; set; }
        public DbSet<Usuarios> Usuarios { get; set; }
        public DbSet<Roles> Roles { get; set; }        
        public DbSet<Categorias> Categorias { get; set; }
        public DbSet<Tipos_Productos> Tipos_Productos { get; set; }
        public DbSet<Productos> Productos { get; set; }
        public DbSet<Unidades_Medida> Unidades_Medida { get; set; }
        public DbSet<Sucursales> Sucursales { get; set; }
        public DbSet<Almacenes> Almacenes { get; set; }
        public DbSet<Inventario> Inventario { get; set; }
        public DbSet<InventarioMovimiento> InventarioMovimientos { get; set; }
        public DbSet<Clientes> Clientes { get; set; }
        public DbSet<Departamentos> Departamentos { get; set; }
        public DbSet<Provincia> Provincias { get; set; }
        public DbSet<Distritos> Distritos { get; set; }
        public DbSet<Pedidos> Pedidos { get; set; }
        public DbSet<PedidoDetalle> PedidoDetalles { get; set; }
        public DbSet<EstadosPedido> EstadosPedido { get; set; }
        public DbSet<ConfiguracionPago> ConfiguracionPagos { get; set; }
        public DbSet<Ventas> Ventas { get; set; }
        public DbSet<Comprobantes> Comprobantes { get; set; }
        public DbSet<ComprobanteDetalle> ComprobanteDetalles { get; set; }
        public DbSet<Pagos> Pagos { get; set; }
        public DbSet<SeriesComprobante> SeriesComprobante { get; set; }
        public DbSet<TiposComprobante> TiposComprobante { get; set; }
        public DbSet<Impuestos> Impuestos { get; set; }
        public DbSet<MetodosPago> MetodosPago { get; set; }
        public DbSet<Plan> Planes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Tenants>(entity =>
            {
                entity.ToTable("tenants");
                entity.HasKey(e => e.tenant_id);
                entity.Property(e => e.plan_fecha_inicio).IsRequired(false);
                entity.Property(e => e.plan_fecha_vencimiento).IsRequired(false);
                entity.HasOne(t => t.plan)
                      .WithMany(p => p.Tenants)
                      .HasForeignKey(t => t.plan_id)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Plan>(entity =>
            {
                entity.ToTable("planes");
                entity.HasKey(e => e.plan_id);
            });

            modelBuilder.Entity<ConfiguracionNegocio>(entity => {
                entity.ToTable("configuracion_negocio");
                entity.HasKey(e => e.config_id);
            });

            modelBuilder.Entity<Departamentos>(entity =>
            {
                entity.ToTable("departamentos");
                entity.HasKey(e => e.departamento_id);
            });

            modelBuilder.Entity<ConfiguracionPago>(entity =>
            {
                entity.ToTable("configuracion_pago");
                entity.HasKey(e => e.config_id);
                entity.Property(e => e.qr_base64).IsRequired(false);
                entity.Property(e => e.numero).IsRequired(false);
                entity.Property(e => e.titular).IsRequired(false);
                entity.Property(e => e.banco).IsRequired(false);
            });

            modelBuilder.Entity<Provincia>(entity =>
            {
                entity.ToTable("provincias");
                entity.HasKey(e => e.provincia_id);

                entity.Property(e => e.departamento_id)
                    .HasColumnName("departamento_id");

                entity.HasOne(p => p.Departamento)
                    .WithMany(d => d.Provincias)
                    .HasForeignKey(p => p.departamento_id)
                    .HasConstraintName("fk_departamentos")
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Distritos>(entity =>
            {
                entity.ToTable("distritos");
                entity.HasKey(e => e.distrito_id);

                entity.HasOne(d => d.Provincia)
                    .WithMany(p => p.Distritos)
                    .HasForeignKey(d => d.provincia_id)
                    .HasConstraintName("fk_provincias")
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProductoSucursal>(entity =>
            {
                entity.ToTable("producto_sucursal");
                entity.HasKey(e => new { e.producto_id, e.sucursal_id });
                entity.Property(e => e.tenant_id).HasColumnName("tenant_id");
                entity.HasOne(ps => ps.productos)
                    .WithMany()
                    .HasForeignKey(ps => ps.producto_id);

                entity.HasOne(ps => ps.sucursales)
                    .WithMany()
                    .HasForeignKey(ps => ps.sucursal_id);
            });

            modelBuilder.Entity<Usuarios>(entity =>
            {
                entity.ToTable("usuarios");
                entity.HasKey(e => e.usuario_id);                
            });           

            modelBuilder.Entity<Roles>(entity =>
            {
                entity.ToTable("roles");
                entity.HasKey(e => e.rol_id);
            });

            modelBuilder.Entity<Clientes>(entity =>
            {
                entity.ToTable("clientes");
                entity.HasKey(e => e.cliente_id);

                entity.HasOne(c => c.departamentos)
                   .WithMany()
                   .HasForeignKey(c => c.departamento_id)
                   .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.provincia)
                    .WithMany()
                    .HasForeignKey(c => c.provincia_id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.distrito)
                    .WithMany()
                    .HasForeignKey(c => c.distrito_id)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Categorias>(entity =>
            {
                entity.ToTable("categorias");
                entity.HasKey(e => e.categoria_id);
            });

            modelBuilder.Entity<Tipos_Productos>(entity =>
            {
                entity.ToTable("tipos_producto");
                entity.HasKey(e => e.tipo_producto_id);
            });

            modelBuilder.Entity<Productos>(entity =>
            {
                entity.ToTable("productos");
                entity.HasKey(e => e.producto_id);
            });

            modelBuilder.Entity<Unidades_Medida>(entity =>
            {
                entity.ToTable("unidades_medida");
                entity.HasKey(e => e.unidad_id);
            });

            modelBuilder.Entity<Sucursales>(entity =>
            {
                entity.ToTable("sucursales");
                entity.HasKey(e => e.sucursal_id);
            });

            modelBuilder.Entity<Almacenes>(entity =>
            {
                entity.ToTable("almacenes");
                entity.HasKey(e => e.almacen_id);
            });

            modelBuilder.Entity<Inventario>(entity =>
            {
                entity.ToTable("inventario");
                entity.HasKey(e => e.inventario_id);
            });

            modelBuilder.Entity<InventarioMovimiento>(entity =>
            {
                entity.ToTable("inventario_movimientos");
                entity.HasKey(e => e.movimiento_id);
            });

            modelBuilder.Entity<Pedidos>(entity =>
            {
               entity.ToTable("pedidos");
                entity.HasKey(e => e.pedido_id);
                // ← Agregar mapeo explícito de los nuevos campos
                entity.Property(e => e.pagado)
                      .HasColumnName("pagado")
                      .HasDefaultValue(false);

                entity.Property(e => e.metodo_pago)
                      .HasColumnName("metodo_pago")
                      .IsRequired(false);
            });

            modelBuilder.Entity<PedidoDetalle>(entity =>
            {
                entity.ToTable("pedido_detalle");
                entity.HasKey(e => e.detalle_id);
            });

            modelBuilder.Entity<EstadosPedido>(entity =>
            {
                entity.ToTable("estados_pedido");
                entity.HasKey(e => e.estado_pedido_id);
            });

            modelBuilder.Entity<Ventas>(entity =>
            {
                entity.ToTable("ventas");
                entity.HasKey(e => e.venta_id);
                entity.HasOne(v => v.clientes)
                      .WithMany().HasForeignKey(v => v.cliente_id);
                entity.HasOne(v => v.usuarios)
                      .WithMany().HasForeignKey(v => v.usuario_id);
                entity.HasOne(v => v.impuestos)
                      .WithMany().HasForeignKey(v => v.impuesto_id);
                entity.HasOne(v => v.pedidos)
                      .WithMany().HasForeignKey(v => v.pedido_id);
            });

            modelBuilder.Entity<Comprobantes>(entity =>
            {
                entity.ToTable("comprobantes");
                entity.HasKey(e => e.comprobante_id);
                entity.HasOne(c => c.ventas)
                      .WithMany(v => v.comprobantes)
                      .HasForeignKey(c => c.venta_id);
                entity.HasOne(c => c.series)
                      .WithMany().HasForeignKey(c => c.serie_id);
                entity.HasOne(c => c.tipos_comprobante)
                      .WithMany().HasForeignKey(c => c.tipo_comprobante_id);
                entity.HasOne(c => c.clientes)
                      .WithMany().HasForeignKey(c => c.cliente_id);
                entity.HasOne(c => c.impuestos)
                      .WithMany().HasForeignKey(c => c.impuesto_id);
            });

            modelBuilder.Entity<ComprobanteDetalle>(entity =>
            {
                entity.ToTable("comprobante_detalle");
                entity.HasKey(e => e.detalle_id);
                entity.HasOne(d => d.comprobantes)
                      .WithMany(c => c.detalles)
                      .HasForeignKey(d => d.comprobante_id);
                entity.HasOne(d => d.productos)
                      .WithMany().HasForeignKey(d => d.producto_id);
            });

            modelBuilder.Entity<Pagos>(entity =>
            {
                entity.ToTable("pagos");
                entity.HasKey(e => e.pago_id);
                entity.HasOne(p => p.ventas)
                      .WithMany(v => v.pagos)
                      .HasForeignKey(p => p.venta_id);
                entity.HasOne(p => p.metodos_pago)
                      .WithMany().HasForeignKey(p => p.metodo_pago_id);
            });

            modelBuilder.Entity<SeriesComprobante>(entity =>
            {
                entity.ToTable("series_comprobante");
                entity.HasKey(e => e.serie_id);
                entity.HasOne(s => s.sucursales)
                      .WithMany().HasForeignKey(s => s.sucursal_id);
                entity.HasOne(s => s.tipos_comprobante)
                      .WithMany().HasForeignKey(s => s.tipo_comprobante_id);
            });

            modelBuilder.Entity<TiposComprobante>(entity =>
            {
                entity.ToTable("tipos_comprobante");
                entity.HasKey(e => e.tipo_comprobante_id);
            });
            
            modelBuilder.Entity<Impuestos>(entity =>
            {
                entity.ToTable("impuestos");
                entity.HasKey(e => e.impuesto_id);
            });

            modelBuilder.Entity<MetodosPago>(entity =>
            {
                entity.ToTable("metodos_pago");
                entity.HasKey(e => e.metodo_pago_id);
            });


            //CONFIGURACIÓN PARA GUARDAR FECHAS EN UTC
            modelBuilder.Entity<Productos>()
            .Property(p => p.created_at)
            .HasConversion(
                v => v,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
            );           


            //RELACIONES PARA JOINS
            modelBuilder.Entity<Productos>()
                .HasOne(p => p.categorias)
                .WithMany()
                .HasForeignKey(p => p.categoria_id);

            modelBuilder.Entity<Productos>()
                .HasOne(p => p.unidades)
                .WithMany()
                .HasForeignKey(p => p.unidad_id);

            modelBuilder.Entity<Productos>()
                .HasOne(p => p.tipos)
                .WithMany()
                .HasForeignKey(p => p.tipo_producto_id);

            modelBuilder.Entity<Usuarios>()
                .HasOne(u => u.roles)
                .WithMany()
                .HasForeignKey(u => u.rol_id);

            modelBuilder.Entity<Usuarios>()
                .HasOne(s => s.sucursales)
                .WithMany()
                .HasForeignKey(s => s.sucursal_id);

            modelBuilder.Entity<Almacenes>()
                .HasOne(s => s.sucursales)
                .WithMany()
                .HasForeignKey(s => s.sucursal_id);

            modelBuilder.Entity<Inventario>()
                .HasOne(i => i.productos)
                .WithMany()
                .HasForeignKey(i => i.producto_id);

            modelBuilder.Entity<Inventario>()
                .HasOne(i => i.almacenes)
                .WithMany()
                .HasForeignKey(i => i.almacen_id);

            modelBuilder.Entity<InventarioMovimiento>()
                .HasOne(i => i.productos)
                .WithMany()
                .HasForeignKey(i => i.producto_id);

            modelBuilder.Entity<InventarioMovimiento>()
                .HasOne(i => i.almacenes)
                .WithMany()
                .HasForeignKey(i => i.almacen_id);    
            
            modelBuilder.Entity<Pedidos>()
                .HasOne(p => p.clientes)
                .WithMany()
                .HasForeignKey(p => p.cliente_id);

            modelBuilder.Entity<Pedidos>()
                .HasOne(p => p.usuarios)
                .WithMany()
                .HasForeignKey(p => p.usuario_id);

            modelBuilder.Entity<Pedidos>()
                .HasOne(p => p.estados_pedidos)
                .WithMany(e => e.pedidos)
                .HasForeignKey(p => p.estado_pedido_id);

            modelBuilder.Entity<Pedidos>()
                .HasOne(p => p.sucursales)
                .WithMany()
                .HasForeignKey(p => p.sucursal_id);

            modelBuilder.Entity<PedidoDetalle>()
                .HasOne(d => d.pedidos)
                .WithMany(p => p.pedido_detalle)
                .HasForeignKey(d => d.pedido_id);

            modelBuilder.Entity<PedidoDetalle>()
                .HasOne(d => d.productos)
                .WithMany()
                .HasForeignKey(d => d.producto_id);

            //INDEXs
            modelBuilder.Entity<Pedidos>()
                .HasIndex(p => new { p.tenant_id, p.estado_pedido_id });

            modelBuilder.Entity<Pedidos>()
                .HasIndex(p => new { p.tenant_id, p.fecha });


            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(TenantEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var method = typeof(DulcesERPContext)
                        .GetMethod(nameof(SetTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.MakeGenericMethod(entityType.ClrType);

                    method?.Invoke(this, new object[] { modelBuilder });
                }
            }

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(DateTime));

                foreach (var property in properties)
                {
                    property.SetValueConverter(
                        new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                            v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
                        )
                    );
                }
            }

        }

        private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
            where TEntity : TenantEntity
        {
            var tenantId = _tenantProvider.GetTenantId();

            modelBuilder.Entity<TEntity>()
                .HasQueryFilter(e => tenantId == 0 || e.tenant_id == tenantId);
        }       

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetTenantId();
            SetTimestamps();
            FixDateTimes();
            return await base.SaveChangesAsync(cancellationToken);
        }        

        private void SetTenantId()
        {
            var tenantId = _tenantProvider.GetTenantId();

            if (tenantId == 0) return;

            var entries = ChangeTracker
                .Entries<TenantEntity>()
                .Where(e => e.State == EntityState.Added);

            foreach (var entry in entries)
            {
                entry.Entity.tenant_id = tenantId;
            }
        }

        private void SetTimestamps()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.State == EntityState.Added);

            foreach (var entry in entries)
            {
                if (entry.Entity is Productos p)
                {
                    p.created_at = DateTime.UtcNow;
                }

                if (entry.Entity is Usuarios u)
                {
                    u.created_at = DateTime.UtcNow;
                }
            }
        }

        private void FixDateTimes()
        {
            var entries = ChangeTracker.Entries();

            foreach (var entry in entries)
            {
                foreach (var prop in entry.Properties)
                {
                    if (prop.Metadata.ClrType == typeof(DateTime))
                    {
                        var value = prop.CurrentValue;

                        if (value == null || (DateTime)value == default)
                        {
                            prop.CurrentValue = DateTime.UtcNow;
                        }
                        else
                        {
                            var dt = (DateTime)value;

                            if (dt.Kind == DateTimeKind.Unspecified)
                                prop.CurrentValue = DateTime.SpecifyKind(dt, DateTimeKind.Utc);

                            if (dt.Kind == DateTimeKind.Local)
                                prop.CurrentValue = dt.ToUniversalTime();
                        }
                    }
                }
            }
        }

    }
}
