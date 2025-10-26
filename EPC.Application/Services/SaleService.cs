using EPC.Domain.Entities;
using EPC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EPC.Application.Services
{
    public class SaleService
    {
        private readonly AppDbContext _context;

        public SaleService(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddSaleAsync(string employeeId, int subCategoryId, decimal amount)
        {
            var subCategory = await _context.SubCategories.FindAsync(subCategoryId);
            if (subCategory == null)
                throw new Exception("SubCategory not found.");

            var commission = amount * subCategory.CommissionRate;

            var sale = new Sale
            {
                EmployeeId = employeeId,
                SubCategoryId = subCategoryId,
                Amount = amount,
                Date = DateTime.Now,
                CalculatedCommission = commission
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Sale>> GetSalesByEmployeeAsync(string employeeId) =>
            await _context.Sales
                .Include(s => s.SubCategory)
                .ThenInclude(sc => sc.Category)
                .Where(s => s.EmployeeId == employeeId)
                .ToListAsync();
    }
}
