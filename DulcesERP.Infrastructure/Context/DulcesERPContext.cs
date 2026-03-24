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
        public DbSet<Usuarios_Roles> Usuarios_Roles { get; set; }
        public DbSet<Categorias> Categorias { get; set; }
        public DbSet<Tipos_Productos> Tipos_Productos { get; set; }
        public DbSet<Productos> Productos { get; set; }
        public DbSet<Unidades_Medida> Unidades_Medida { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tenants>(entity =>
            {
                entity.ToTable("tenants");
                entity.HasKey(e => e.tenant_id);
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

            modelBuilder.Entity<Usuarios_Roles>(entity =>
            {
                entity.ToTable("usuarios_roles");
                entity.HasKey(e => new { e.usuario_id, e.rol_id });
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
        }

        //private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
        //    where TEntity : TenantEntity
        //{
        //    modelBuilder.Entity<TEntity>()
        //        .HasQueryFilter(e => e.tenant_id == _tenantProvider.GetTenantId());
        //}

        private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
            where TEntity : TenantEntity
        {
            var tenantId = _tenantProvider.GetTenantId();

            modelBuilder.Entity<TEntity>()
                .HasQueryFilter(e => tenantId == 0 || e.tenant_id == tenantId);
        }

        public override int SaveChanges()
        {
            SetTenantId();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetTenantId();
            return await base.SaveChangesAsync(cancellationToken);
        }

        //private void SetTenantId()
        //{
        //    var tenantId = _tenantProvider.GetTenantId();

        //    var entries = ChangeTracker
        //        .Entries<TenantEntity>()
        //        .Where(e => e.State == EntityState.Added);

        //    foreach (var entry in entries)
        //    {
        //        entry.Entity.tenant_id = tenantId;
        //    }
        //}

        private void SetTenantId()
        {
            var tenantId = _tenantProvider.GetTenantId();

            if (tenantId == 0)
                return;

            var entries = ChangeTracker
                .Entries<TenantEntity>()
                .Where(e => e.State == EntityState.Added);

            foreach (var entry in entries)
            {
                entry.Entity.tenant_id = tenantId;
            }
        }
    }
}
