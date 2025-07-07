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
                .OrderByDescending(o => o.OrderedAt)
                .Select(o => new MaterialOrderResponse
                {
                    OrderId = o.OrderId,
                    SupplierId = o.SupplierId,
                    SupplierName = o.Supplier != null ? o.Supplier.CompanyName : null,
                    OrderedAt = o.OrderedAt,
                    ReceivedAt = o.ReceivedAt,
                    Status = o.ReceivedAt == null ? "PENDING" : "COMPLETED",
                    OrderStatusId = o.OrderStatusId,
                    Items = new List<MaterialOrderItemResponse> {
                        new MaterialOrderItemResponse {
                            MaterialId = o.MaterialId,
                            MaterialName = o.Material != null ? o.Material.MaterialName : string.Empty,
                            Amount = o.RemainingAmount
                        }
                    }
                })
                .ToListAsync();
        }

        public async Task<MaterialOrderResponse?> GetMaterialOrderByIdAsync(int orderId)
        {
            return await _context.MaterialOrders
                .Include(o => o.Supplier)
                .Include(o => o.Material)
                .Where(o => o.OrderId == orderId)
                .Select(o => new MaterialOrderResponse
                {
                    OrderId = o.OrderId,
                    SupplierId = o.SupplierId,
                    SupplierName = o.Supplier != null ? o.Supplier.CompanyName : null,
                    OrderedAt = o.OrderedAt,
                    ReceivedAt = o.ReceivedAt,
                    Status = o.ReceivedAt == null ? "PENDING" : "COMPLETED",
                    OrderStatusId = o.OrderStatusId,
                    Items = new List<MaterialOrderItemResponse> {
                        new MaterialOrderItemResponse {
                            MaterialId = o.MaterialId,
                            MaterialName = o.Material != null ? o.Material.MaterialName : string.Empty,
                            Amount = o.RemainingAmount
                        }
                    }
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
                "CALL create_material_order(@p_supplier_id, @p_material_id, @p_remaining_amount, @p_created_order_id)",
                new NpgsqlParameter("p_supplier_id", request.SupplierId),
                new NpgsqlParameter("p_material_id", request.Items[0].MaterialId),
                new NpgsqlParameter("p_remaining_amount", request.Items[0].Amount),
                createdOrderIdParam
            );

            if (createdOrderIdParam.Value != DBNull.Value && createdOrderIdParam.Value is int newOrderId)
            {
                // Set OrderedAt to the current simulation day
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