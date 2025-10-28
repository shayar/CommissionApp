// EPC.WEB/Pages/Sales/HistoryModel.cshtml.cs (COMPLETE CODE with Logging)

using EPC.Application.Dtos;
using EPC.Domain.Entities;
using EPC.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Serilog;
using EPC.Application.Services;

namespace EPC.WEB.Pages.Sales
{
    [Authorize]
    public class HistoryModel : PageModel
    {
        private readonly ISaleService _saleService;
        private readonly AppDbContext _context; // Used for populating Category dropdowns
        private readonly Serilog.ILogger _logger;

        public HistoryModel(ISaleService saleService, AppDbContext context, Serilog.ILogger logger)
        {
            _saleService = saleService;
            _context = context;
            _logger = logger.ForContext<HistoryModel>(); // Initialize context-aware logger
        }

        // Output Data
        public List<SaleItemDto> Sales { get; set; } = new List<SaleItemDto>();
        public decimal TotalSales { get; set; }
        public decimal TotalCommission { get; set; }

        // Input Filters (Bound from the form via GET)
        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterCategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterSubCategoryId { get; set; }

        // Dropdown Data
        public List<Category> Categories { get; set; } = new List<Category>();

        public async Task OnGetAsync()
        {
            var appUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(appUserId))
            {
                _logger.Warning("User ID not found in claims during History load.");
                return;
            }

            try
            {
                // Load all categories for filter dropdowns
                Categories = await _context.Categories
                    .Include(c => c.SubCategories)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                _logger.Information("Loading sales history for user {UserId}. Filters: Start={Start}, End={End}, CategoryId={CatId}",
                    appUserId, StartDate, EndDate, FilterCategoryId);

                // Construct Filter DTO
                var filters = new SaleFilterDto
                {
                    StartDate = StartDate,
                    EndDate = EndDate,
                    CategoryId = FilterCategoryId,
                    SubCategoryId = FilterSubCategoryId
                };

                // Retrieve data via Application Service
                var history = await _saleService.GetEmployeeSalesHistoryAsync(appUserId, filters);

                Sales = history.Sales;
                TotalSales = history.TotalSalesAmount;
                TotalCommission = history.TotalCommissionAmount;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to retrieve sales history for user {UserId}.", appUserId);
                // Gracefully degrade the UI on failure
                Sales = new List<SaleItemDto>();
                TempData["ErrorMessage"] = "Could not load sales history due to a system error.";
            }
        }
    }
}