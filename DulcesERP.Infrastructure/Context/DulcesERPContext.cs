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
        

        public DbSet<Tenants> Tenants { get; set; }
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


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            modelBuilder.Entity<Tenants>(entity =>
            {
                entity.ToTable("tenants");
                entity.HasKey(e => e.tenant_id);
            });

            modelBuilder.Entity<Departamentos>(entity =>
            {
                entity.ToTable("departamentos");
                entity.HasKey(e => e.departamento_id);
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

            if (tenantId == 0)
                throw new Exception("Tenant no definido");

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
