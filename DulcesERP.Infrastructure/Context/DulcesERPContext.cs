using Microsoft.EntityFrameworkCore;
using DulcesERP.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DulcesERP.Infrastructure.Context
{
    public class DulcesERPContext : DbContext
    {
        public DulcesERPContext(DbContextOptions<DulcesERPContext> options) : base(options)
        {
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
        }
    }
}
