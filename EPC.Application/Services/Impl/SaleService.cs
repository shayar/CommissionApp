using EPC.Application.Dtos;
using EPC.Domain.Entities;
using EPC.Infrastructure.Data;
using EPC.Infrastructure.Identity;
using EPC.Infrastructure.Repos;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EPC.Application.Services.Impl
{
    public class SaleService : ISaleService
    {
        // NOTE: AppDbContext is injected here solely to perform data retrieval/lookup of Categories
        // which, in a perfect model, would be done by an ICategoryRepository contract.
        private readonly AppDbContext _context;
        private readonly IUserRepository _userRepository;
        private readonly ISaleRepository _saleRepository;
        private readonly ILogger _logger;

        public SaleService(AppDbContext context, IUserRepository userRepository, ISaleRepository saleRepository, ILogger logger)
        {
            _context = context;
            _userRepository = userRepository;
            _saleRepository = saleRepository;
            _logger = logger.ForContext<SaleService>();
        }

        public async Task LogSaleAsync(string appUserId, int categoryOrSubCategoryId, decimal amount, bool isSubCategory,
                                       string paymentType, string? trackingNumber, string? description)
        {
            decimal commissionRate = 0;
            string saleDescription = "";

            var employeeUser = await _userRepository.GetAppUserByIdAsync(appUserId);

            if (employeeUser == null)
                throw new KeyNotFoundException($"User ID {appUserId} not found in Identity.");

            try
            {
                // 1. DETERMINE COMMISSION RATE
                if (isSubCategory)
                {
                    var subCategory = await _context.SubCategories
                        .Include(sc => sc.Category)
                        .FirstOrDefaultAsync(sc => sc.Id == categoryOrSubCategoryId);

                    if (subCategory == null)
                        throw new KeyNotFoundException($"SubCategory ID {categoryOrSubCategoryId} not found.");

                    commissionRate = subCategory.CommissionRate;
                    saleDescription = $"Sale under SubCategory: {subCategory.Category.Name} - {subCategory.Name}";
                }
                else // It's a top-level Category (uses Category.CommissionRate)
                {
                    var category = await _context.Categories.FindAsync(categoryOrSubCategoryId);

                    if (category == null || !category.CommissionRate.HasValue)
                        throw new InvalidOperationException($"Category ID {categoryOrSubCategoryId} is not commissionable or not found.");

                    commissionRate = category.CommissionRate.Value;
                    saleDescription = $"Sale under Category: {category.Name}";
                }

                if (commissionRate <= 0)
                {
                    throw new InvalidOperationException($"Commission rate for the selected item is zero.");
                }

                var calculatedCommission = amount * commissionRate;

                // 2. CREATE ENTITIES
                var sale = new Sale
                {
                    AppUserId = appUserId,
                    EmployeeId = employeeUser.EmployeeId ?? appUserId,
                    Amount = amount,
                    Date = DateTime.UtcNow,
                    CalculatedCommission = calculatedCommission,
                    SubCategoryId = isSubCategory ? categoryOrSubCategoryId : 0,

                    // NEW FIELDS
                    PaymentType = paymentType,
                    TrackingNumber = trackingNumber,
                    Description = description,
                };

                var audit = new Audit
                {
                    Action = $"{saleDescription}. Amt: {amount:C2}, Comm: {calculatedCommission:C2}. Type: {paymentType}{(trackingNumber != null ? ", Tracking: " + trackingNumber : "")}",
                    PerformedBy = employeeUser.Email ?? appUserId
                };

                // 3. PERSIST VIA REPOSITORY CONTRACT
                await _saleRepository.AddSaleAsync(sale, audit);

                _logger.Information("Sale logged successfully. User: {User}, Description: {Desc}, Amount: {Amt}", appUserId, saleDescription, amount);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to log sale for user {User}", appUserId);
                throw;
            }
        }

        public async Task<SalesHistoryDto> GetEmployeeSalesHistoryAsync(string appUserId, SaleFilterDto filters)
        {
            var query = _context.Sales
                .Where(s => s.AppUserId == appUserId)
                .Include(s => s.SubCategory)
                .ThenInclude(sc => sc.Category)
                .AsNoTracking();

            // 1. Apply Date Filters
            if (filters.StartDate.HasValue)
            {
                query = query.Where(s => s.Date >= filters.StartDate.Value.Date);
            }
            if (filters.EndDate.HasValue)
            {
                // Include the entire end date day
                query = query.Where(s => s.Date < filters.EndDate.Value.Date.AddDays(1));
            }

            // 2. Apply Category/SubCategory Filters
            // Note: This relies on SubCategoryId storing the Category ID (0) for top-level sales, which isn't ideal,
            // but we'll filter on the Category/SubCategory names/IDs attached to the sale.
            if (filters.CategoryId.HasValue)
            {
                // Filter by the parent category ID
                query = query.Where(s => s.SubCategory.CategoryId == filters.CategoryId.Value ||
                                         (!s.SubCategory.Category.SubCategories.Any() && s.SubCategoryId == filters.CategoryId.Value));
            }
            if (filters.SubCategoryId.HasValue)
            {
                // Filter by the specific SubCategory ID
                query = query.Where(s => s.SubCategoryId == filters.SubCategoryId.Value);
            }

            var sales = await query.OrderByDescending(s => s.Date).ToListAsync();

            // 3. Project to DTO
            var saleDtos = sales.Select(s => new SaleItemDto
            {
                Date = s.Date,
                CategoryName = s.SubCategory.Category.Name,
                // If SubCategoryId is 0 (or maps to Category ID), display only Category Name (complex logic simplified for projection)
                SubCategoryName = s.SubCategory.Id > 0 ? s.SubCategory.Name : "N/A",
                Amount = s.Amount,
                CommissionEarned = s.CalculatedCommission,
                PaymentType = s.PaymentType,
                Description = s.Description,
                TrackingNumber = s.TrackingNumber,
            }).ToList();

            // 4. Calculate Totals
            return new SalesHistoryDto
            {
                Sales = saleDtos,
                TotalSalesAmount = sales.Sum(s => s.Amount),
                TotalCommissionAmount = sales.Sum(s => s.CalculatedCommission)
            };
        }

        public async Task<SalesReportDto> GetAdminSalesReportAsync(AdminReportFilter filters)
        {
            // 1. --- EXECUTE QUERIES ---

            // Step 1a: Query and Filter Sales Data (Loading only Domain entities and foreign keys)
            var salesQuery = _context.Sales
                .Include(s => s.SubCategory)
                    .ThenInclude(sc => sc.Category)
                .AsNoTracking();

            // Apply Filters (Filtering logic remains correct)
            if (!string.IsNullOrEmpty(filters.AppUserId))
                salesQuery = salesQuery.Where(s => s.AppUserId == filters.AppUserId);
            if (filters.StartDate.HasValue)
                salesQuery = salesQuery.Where(s => s.Date >= filters.StartDate.Value);
            if (filters.EndDate.HasValue)
                salesQuery = salesQuery.Where(s => s.Date < filters.EndDate.Value.AddDays(1));
            if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
            {
                string searchLower = filters.SearchTerm.ToLower();
                salesQuery = salesQuery.Where(s =>
                    s.SubCategory.Name.ToLower().Contains(searchLower) ||
                    s.SubCategory.Category.Name.ToLower().Contains(searchLower) ||
                    (s.Description != null && s.Description.ToLower().Contains(searchLower)));
            }

            var sales = await salesQuery.OrderByDescending(s => s.Date).ToListAsync();

            // Step 1b: Get unique User IDs from the filtered sales list
            var uniqueUserIds = sales.Select(s => s.AppUserId).Distinct().ToList();

            // Step 1c: Look up all required user details (Infrastructure entity)
            // We project a simple structure to minimize data transfer
            var userLookup = await _context.Users
                .Where(u => uniqueUserIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email
                })
                .ToDictionaryAsync(u => u.Id);

            // 2. --- AGGREGATION AND PROJECTION (In Memory) ---

            var totalSales = sales.Sum(s => s.Amount);
            var totalCommission = sales.Sum(s => s.CalculatedCommission);

            var totalsByCategory = sales
                .GroupBy(s => s.SubCategory.Category.Name)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.Amount));

            var salesDetails = sales.Select(s =>
            {
                // Retrieve user data from the in-memory lookup dictionary
                var userData = userLookup.GetValueOrDefault(s.AppUserId);

                return new ReportSaleDetailDto
                {
                    Date = s.Date,
                    // FIX: Access fields directly from the loaded anonymous object
                    EmployeeName = userData?.FullName ?? userData?.Email ?? "Unknown Employee",
                    CategoryName = s.SubCategory.Category.Name,
                    SubCategoryName = s.SubCategory.Name,
                    Amount = s.Amount,
                    CommissionEarned = s.CalculatedCommission,
                    PaymentType = s.PaymentType,
                    TrackingNumber = s.TrackingNumber,
                    Description = s.Description,
                };
            }).ToList();

            // 3. Return DTO
            return new SalesReportDto
            {
                SalesDetails = salesDetails,
                TotalsByCategory = totalsByCategory,
                GrandTotalSales = totalSales,
                GrandTotalCommission = totalCommission
            };
        }
    }
}
