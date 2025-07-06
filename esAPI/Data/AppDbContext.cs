using Microsoft.EntityFrameworkCore;
using esAPI.Models;

namespace esAPI.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Material> Materials { get; set; }
        public DbSet<Supply> Supplies { get; set; }
        public DbSet<Machine> Machines { get; set; }
        public DbSet<MachineRatio> MachineRatios { get; set; }
        public DbSet<MaterialSupplier> MaterialSuppliers { get; set; }
        public DbSet<MaterialOrder> MaterialOrders { get; set; }
        public DbSet<PhoneManufacturer> PhoneManufacturers { get; set; }
        public DbSet<Electronic> Electronics { get; set; }
        public DbSet<ElectronicsOrder> ElectronicsOrders { get; set; }
        
        public DbSet<MachineStatuses> MachineStatuses { get; set; }
        public DbSet<CurrentSupply> CurrentSupplies { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CurrentSupply>().ToView("current_supplies");
        }
        
    }
}