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

        private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
            where TEntity : TenantEntity
        {
            modelBuilder.Entity<TEntity>()
                .HasQueryFilter(e => e.tenant_id == _tenantProvider.GetTenantId());
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

        private void SetTenantId()
        {
            var tenantId = _tenantProvider.GetTenantId();

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
