using Microsoft.EntityFrameworkCore;
using esAPI.Models;

namespace esAPI.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Company> Companies { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<MaterialSupply> MaterialSupplies { get; set; }
        public DbSet<MaterialOrder> MaterialOrders { get; set; }

        public DbSet<Machine> Machines { get; set; }
        public DbSet<MachineOrder> MachineOrders { get; set; }
        public DbSet<MachineRatio> MachineRatios { get; set; }
        public DbSet<MachineStatus> MachineStatuses { get; set; }
        public DbSet<MachineDetails> MachineDetails { get; set; }

        public DbSet<Electronic> Electronics { get; set; }
        public DbSet<ElectronicsOrder> ElectronicsOrders { get; set; }
        public DbSet<ElectronicsStatus> ElectronicsStatuses { get; set; }

        public DbSet<OrderStatus> OrderStatuses { get; set; }
        public DbSet<LookupValue> LookupValues { get; set; }
        public DbSet<Simulation> Simulations { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);
        }
    }
}