using EPC.Domain.Entities;
using EPC.Infrastructure.Data;

namespace EPC.Infrastructure.Repos.Impl
{
    public class SaleRepository : ISaleRepository
    {
        private readonly AppDbContext _context;

        public SaleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddSaleAsync(Sale sale, Audit audit)
        {
            _context.Sales.Add(sale);
            _context.AuditLogs.Add(audit);
            await _context.SaveChangesAsync();
        }
    }
}
