using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FerreteriaApp.Models;

namespace FerreteriaApp.Data
{
    // Único DbContext: Identity (Usuarios/Roles) + tablas de la ferretería.
    // IdentityDbContext<Usuario, Rol, int>: usuarios y roles propios con clave numérica.
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<Usuario, Rol, int>(options)
    {
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Inventario> Inventarios { get; set; }
        public DbSet<FormaPago> FormasPago { get; set; }
        public DbSet<Venta> Ventas { get; set; }
        public DbSet<DetalleVenta> DetalleVentas { get; set; }
        public DbSet<Compra> Compras { get; set; }
        public DbSet<DetalleCompra> DetalleCompras { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---- Tablas de Identity con los nombres del diagrama ----
            modelBuilder.Entity<Usuario>(e =>
            {
                e.ToTable("Usuarios");
                e.Property(u => u.Id).HasColumnName("IdUsuario");
            });
            modelBuilder.Entity<Rol>(e =>
            {
                e.ToTable("Roles");
                e.Property(r => r.Id).HasColumnName("IdRol");
                e.Property(r => r.Name).HasColumnName("Nombre");
            });
            modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UsuarioRoles");
            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UsuarioClaims");
            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UsuarioLogins");
            modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UsuarioTokens");
            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RolClaims");

            // ---- Herencia Usuario -> Empleado / Cliente (TPT: una tabla por tipo) ----
            // La clave de Empleados y Clientes es a la vez FK a Usuarios.IdUsuario.
            modelBuilder.Entity<Empleado>().ToTable("Empleados", t => t.Property(e => e.Id).HasColumnName("IdEmpleado"));
            modelBuilder.Entity<Cliente>().ToTable("Clientes", t => t.Property(c => c.Id).HasColumnName("IdCliente"));

            // ---- Precisión de importes (2 decimales) ----
            modelBuilder.Entity<Producto>().Property(p => p.PrecioVenta).HasPrecision(18, 2);
            modelBuilder.Entity<Producto>().Property(p => p.PrecioCompra).HasPrecision(18, 2);
            modelBuilder.Entity<Venta>().Property(v => v.Total).HasPrecision(18, 2);
            modelBuilder.Entity<DetalleVenta>().Property(d => d.PrecioUnitario).HasPrecision(18, 2);
            modelBuilder.Entity<DetalleVenta>().Property(d => d.Subtotal).HasPrecision(18, 2);
            modelBuilder.Entity<Compra>().Property(c => c.Total).HasPrecision(18, 2);
            modelBuilder.Entity<DetalleCompra>().Property(d => d.PrecioUnitario).HasPrecision(18, 2);
            modelBuilder.Entity<DetalleCompra>().Property(d => d.Subtotal).HasPrecision(18, 2);

            // ---- Relaciones ----

            // Categoría 1 --- 0..* Producto (contiene). No se puede borrar una categoría con productos.
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Categoria).WithMany(c => c.Productos)
                .HasForeignKey(p => p.IdCategoria).OnDelete(DeleteBehavior.Restrict);

            // Proveedor 1 --- 0..* Producto (suministra)
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Proveedor).WithMany(pr => pr.Productos)
                .HasForeignKey(p => p.IdProveedor).OnDelete(DeleteBehavior.Restrict);

            // Producto 1 --- 1..* Inventario (tiene). Si se borra el producto, se borra su inventario.
            modelBuilder.Entity<Inventario>()
                .HasOne(i => i.Producto).WithMany(p => p.Inventarios)
                .HasForeignKey(i => i.IdProducto).OnDelete(DeleteBehavior.Cascade);

            // Cliente 1 --- 0..* Venta (realiza)
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Cliente).WithMany(c => c.Ventas)
                .HasForeignKey(v => v.IdCliente).OnDelete(DeleteBehavior.Restrict);

            // Empleado 1 --- 0..* Venta (registra). Opcional: una venta web queda sin empleado hasta completarse.
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.Empleado).WithMany()
                .HasForeignKey(v => v.IdEmpleado).OnDelete(DeleteBehavior.Restrict);

            // FormaPago 1 --- 0..* Venta (se paga con)
            modelBuilder.Entity<Venta>()
                .HasOne(v => v.FormaPago).WithMany(f => f.Ventas)
                .HasForeignKey(v => v.IdFormaPago).OnDelete(DeleteBehavior.Restrict);

            // Venta 1 --- 1..* DetalleVenta (tiene). Los detalles se borran con la venta.
            modelBuilder.Entity<DetalleVenta>()
                .HasOne(d => d.Venta).WithMany(v => v.Detalles)
                .HasForeignKey(d => d.IdVenta).OnDelete(DeleteBehavior.Cascade);

            // DetalleVenta 0..* --- 1 Producto (es de). No se puede borrar un producto vendido.
            modelBuilder.Entity<DetalleVenta>()
                .HasOne(d => d.Producto).WithMany()
                .HasForeignKey(d => d.IdProducto).OnDelete(DeleteBehavior.Restrict);

            // Proveedor 1 --- 0..* Compra
            modelBuilder.Entity<Compra>()
                .HasOne(c => c.Proveedor).WithMany(p => p.Compras)
                .HasForeignKey(c => c.IdProveedor).OnDelete(DeleteBehavior.Restrict);

            // Empleado 1 --- 0..* Compra (la realiza)
            modelBuilder.Entity<Compra>()
                .HasOne(c => c.Empleado).WithMany()
                .HasForeignKey(c => c.IdEmpleado).OnDelete(DeleteBehavior.Restrict);

            // Compra 1 --- 1..* DetalleCompra (tiene)
            modelBuilder.Entity<DetalleCompra>()
                .HasOne(d => d.Compra).WithMany(c => c.Detalles)
                .HasForeignKey(d => d.IdCompra).OnDelete(DeleteBehavior.Cascade);

            // DetalleCompra 0..* --- 1 Producto
            modelBuilder.Entity<DetalleCompra>()
                .HasOne(d => d.Producto).WithMany()
                .HasForeignKey(d => d.IdProducto).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
