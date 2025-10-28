
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
                var categorySales = await _context.Sales
                    .Where(s => s.Date >= startDate)
                    .Include(s => s.SubCategory)
                    .ThenInclude(sc => sc.Category)
                    .AsNoTracking()
                    .Select(s => new
                    {
                        CategoryName = s.SubCategory.Category.Name,
                        Amount = s.Amount
                    })
                    .GroupBy(x => x.CategoryName)
                    .Select(g => new CategorySalesDto
                    {
                        CategoryName = g.Key,
                        TotalSales = (decimal)g.Sum(x => (double)x.Amount)
                    })
                    .ToListAsync();

                var rankedSales = categorySales
                    .OrderByDescending(d => d.TotalSales)
                    .ToList();

                _logger.Information("Generated sales by category report from {StartDate}. Found {Count} categories.", startDate.ToShortDateString(), rankedSales.Count);
                return rankedSales;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "FATAL ERROR: Failed to translate complex sales category query. Database query failed.");
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
                        TotalSales = (decimal)g.Sum(s => (double)s.Amount),
                        TotalCommission = (decimal)g.Sum(s => (double)s.CalculatedCommission)
                    })
                    .ToListAsync();

                var rankedData = performanceData
                    .OrderByDescending(d => d.TotalSales)
                    .Take(count)
                    .ToList(); // Execute to List<T> in memory before returning

                _logger.Information("Generated top {Count} employee performance list from {StartDate}", rankedData.Count, startDate.ToShortDateString());
                return rankedData; // Return the concrete list (Task<List<T>> is implicitly handled by the async signature)
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error fetching top employee performance data. SQLite decimal issue suspected.", ex);
                return new List<EmployeePerformanceDto>();
            }
        }
    }
}
