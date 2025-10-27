
using EPC.Application.Dtos;
using EPC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EPC.Application.Services.Impl
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;
        private readonly ILogger _logger;

        public DashboardService(AppDbContext context, ILogger logger)
        {
            _context = context;
            _logger = logger.ForContext<DashboardService>();
        }
        public async Task<EmployeeSummaryDto> GetEmployeeSummaryAsync(string userId, DateTime startDate)
        {
            try
            {
                var sales = await _context.Sales
                    // Ensure you are filtering by the correct ID property (AppUserId on Sale entity)
                    .Where(s => s.AppUserId == userId && s.Date >= startDate)
                    .AsNoTracking()
                    .ToListAsync();

                var summary = new EmployeeSummaryDto
                {
                    TotalSales = sales.Sum(s => s.Amount),
                    TotalCommission = sales.Sum(s => s.CalculatedCommission),
                    SaleCount = sales.Count
                };

                _logger.Information("Generated employee summary for user {UserId}. Sales: {Count}", userId, summary.SaleCount);
                return summary;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error fetching employee summary for user {UserId}", userId);
                // Return default/empty DTO on error to allow the page to render gracefully
                return new EmployeeSummaryDto();
            }
        }

        public async Task<List<CategorySalesDto>> GetSalesByCategoryAsync(DateTime startDate)
        {
            try
            {
                // Note: This relies on the SubCategory -> Category navigation property being correctly
                // configured in AppDbContext to allow EF Core to translate the nested join to SQL.
                var categorySales = await _context.Sales
                    .Include(s => s.SubCategory)
                    .ThenInclude(sc => sc.Category)
                    .Where(s => s.Date >= startDate)
                    .GroupBy(s => s.SubCategory.Category.Name)
                    .Select(g => new CategorySalesDto
                    {
                        CategoryName = g.Key,
                        TotalSales = g.Sum(s => s.Amount)
                    })
                    .OrderByDescending(d => d.TotalSales)
                    .AsNoTracking()
                    .ToListAsync();

                _logger.Information("Generated sales by category report from {StartDate}", startDate.ToShortDateString());
                return categorySales;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error fetching sales by category data.");
                return new List<CategorySalesDto>();
            }
        }

        public async Task<List<EmployeePerformanceDto>> GetTopEmployeePerformanceAsync(DateTime startDate, int count = 5)
        {
            try
            {
                var performanceData = await _context.Sales
                    .Where(s => s.Date >= startDate)
                    .GroupBy(s => s.AppUserId)
                    .Select(g => new EmployeePerformanceDto
                    {
                        AppUserId = g.Key,
                        TotalSales = g.Sum(s => s.Amount),
                        TotalCommission = g.Sum(s => s.CalculatedCommission)
                    })
                    .OrderByDescending(d => d.TotalSales)
                    .Take(count)
                    .AsNoTracking()
                    .ToListAsync();

                _logger.Information("Generated top {Count} employee performance list from {StartDate}", count, startDate.ToShortDateString());
                return performanceData;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error fetching top employee performance data.");
                return new List<EmployeePerformanceDto>();
            }
        }
    }
}
