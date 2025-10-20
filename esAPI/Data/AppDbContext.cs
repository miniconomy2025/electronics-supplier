using Microsoft.EntityFrameworkCore;
using esAPI.Models;
using SimulationModel = esAPI.Models.Simulation;
using DisasterModel = esAPI.Models.Disaster;

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

        public DbSet<PickupRequest> PickupRequests { get; set; }

        public DbSet<OrderStatus> OrderStatuses { get; set; }
        public DbSet<LookupValue> LookupValues { get; set; }
        public DbSet<SimulationModel> Simulations { get; set; }
        public DbSet<CurrentSupply> CurrentSupplies { get; set; }
        public DbSet<DisasterModel> Disasters { get; set; }
        public DbSet<BankBalanceSnapshot> BankBalanceSnapshots { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public DbSet<EffectiveMaterialStock> EffectiveMaterialStock { get; set; }

        public DbSet<DailyMaterialConsumption> DailyMaterialConsumption { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CurrentSupply>().ToView("current_supplies");
            modelBuilder.Entity<EffectiveMaterialStock>().ToView("effective_material_stock");
            modelBuilder.Entity<DailyMaterialConsumption>().ToView("daily_material_consumption");
        }
    }
}
