using esAPI.Clients;
using esAPI.DTOs;
using esAPI.Interfaces;

namespace esAPI.Services;

public record SourcedSupplier(
    string Name,
    object Client,
    SupplierMaterialInfo MaterialDetails
);

public interface IMaterialSourcingService
{
    Task<SourcedSupplier?> FindBestSupplierAsync(string materialName);
}