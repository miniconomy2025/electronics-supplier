using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using esAPI.Data;
using esAPI.DTOs.MaterialOrder;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace esAPI.Services
{
    public class MaterialOrderService : IMaterialOrderService
    {
        private readonly AppDbContext _context;
        public MaterialOrderService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MaterialOrderResponse>> GetAllMaterialOrdersAsync()
        {
            return await _context.MaterialOrders
            .Include(o => o.Supplier)
            .Include(o => o.Material)
            .Include(o => o.OrderStatus)
            .OrderByDescending(o => o.OrderedAt)
            .Select(o => new MaterialOrderResponse
            {
                OrderId = o.OrderId,
                SupplierId = o.SupplierId,
                SupplierName = o.Supplier!.CompanyName,
                ExternalOrderId = o.ExternalOrderId,
                MaterialId = o.MaterialId,
                MaterialName = o.Material!.MaterialName,
                RemainingAmount = o.RemainingAmount,
                Status = o.OrderStatus!.Status,
                OrderedAt = o.OrderedAt,
                ReceivedAt = o.ReceivedAt
            })
            .ToListAsync();
        }

        public async Task<MaterialOrderResponse?> GetMaterialOrderByIdAsync(int orderId)
        {
            return await _context.MaterialOrders
                .Include(o => o.Supplier)
                .Include(o => o.Material)
                .Include(o => o.OrderStatus)
                .Where(o => o.OrderId == orderId)
                .OrderByDescending(o => o.OrderedAt)
                .Select(o => new MaterialOrderResponse
                {
                    OrderId = o.OrderId,
                    SupplierId = o.SupplierId,
                    SupplierName = o.Supplier!.CompanyName,
                    ExternalOrderId = o.ExternalOrderId,
                    MaterialId = o.MaterialId,
                    MaterialName = o.Material!.MaterialName,
                    RemainingAmount = o.RemainingAmount,
                    Status = o.OrderStatus!.Status,
                    OrderedAt = o.OrderedAt,
                    ReceivedAt = o.ReceivedAt
                })
                .SingleOrDefaultAsync();
        }

        public async Task<MaterialOrderResponse> CreateMaterialOrderAsync(CreateMaterialOrderRequest request)
        {
            // Get current simulation day
            var sim = _context.Simulations.FirstOrDefault(s => s.IsRunning);
            if (sim == null)
                throw new InvalidOperationException("Simulation not running.");

            // This assumes a single item per order for now, as in the original controller
            var createdOrderIdParam = new NpgsqlParameter("p_created_order_id", DbType.Int32)
            {
                Direction = ParameterDirection.InputOutput,
                Value = DBNull.Value
            };

            await _context.Database.ExecuteSqlRawAsync(
                "CALL create_material_order(@p_supplier_id, @p_material_id, @p_amount, @p_current_day, @p_created_order_id)",
                new NpgsqlParameter("p_supplier_id", request.SupplierId),
                new NpgsqlParameter("p_material_id", request.MaterialId),
                new NpgsqlParameter("p_amount", request.Amount),
                new NpgsqlParameter("p_current_day", sim.DayNumber),
                createdOrderIdParam
            );

            if (createdOrderIdParam.Value != DBNull.Value && createdOrderIdParam.Value is int newOrderId)
            {
                var newOrder = await _context.MaterialOrders.FindAsync(newOrderId);
                if (newOrder != null)
                {
                    newOrder.OrderedAt = sim.DayNumber;
                    await _context.SaveChangesAsync();
                }
                var response = await GetMaterialOrderByIdAsync(newOrderId);
                if (response != null)
                    return response;
            }
            throw new InvalidOperationException("Could not retrieve the new order ID after creation.");
        }

        public async Task<bool> UpdateMaterialOrderAsync(int orderId, UpdateMaterialOrderRequest request)
        {
            var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
                $"CALL update_material_order({orderId}, {request.SupplierId}, {request.OrderedAt}, {request.ReceivedAt})");
            return affected > 0;
        }
    }
}