using EPC.Domain.Entities;

namespace EPC.Infrastructure.Repos
{
    public interface ISaleRepository
    {
        Task AddSaleAsync(Sale sale, Audit audit);
    }
}
