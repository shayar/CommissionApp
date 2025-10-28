
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using EPC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using EPC.Infrastructure.Identity;
using Serilog;
using EPC.Application.Dtos;
using EPC.Application.Services;
using System.Text;

namespace EPC.WEB.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ReportsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ISaleService _saleService; // Application Interface dependency
        private readonly Serilog.ILogger _logger;

        public ReportsModel(AppDbContext context, ISaleService saleService, Serilog.ILogger logger)
        {
            _context = context;
            _saleService = saleService;
            _logger = logger.ForContext<ReportsModel>();
        }

        // Output Data
        public SalesReportDto ReportData { get; set; } = new();
        public List<AppUser> Employees { get; set; } = new();

        // Input Filters (Bound from the form via GET)
        [BindProperty(SupportsGet = true)] public string? AppUserId { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? EndDate { get; set; }
        [BindProperty(SupportsGet = true)] public string? Search { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                // Load all employees (Admin, Employee, Developer roles) for the filter dropdown
                Employees = await _context.Users.Where(u => u.Email != "admin@epc.com").ToListAsync();

                // 1. Construct Filter DTO
                var filters = new AdminReportFilter
                {
                    AppUserId = AppUserId,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    SearchTerm = Search
                };

                // 2. Retrieve Report Data from Application Service
                ReportData = await _saleService.GetAdminSalesReportAsync(filters);

                _logger.Information("Admin viewed sales report. Total Sales: {TotalSales}. Filters: Employee: {Employee}, Start: {Start}, End: {End}, Search: {Search}",
                    ReportData.GrandTotalSales.ToString("C2"), AppUserId, StartDate, EndDate, Search);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load sales reports for admin.");
                ModelState.AddModelError(string.Empty, "Something went wrong while loading the report. Please try again.");
            }
        }

        public async Task<IActionResult> OnPostExportExcel()
        {
            // Simple method to force download of data (detailed implementation below)
            await OnGetAsync();

            // Check for immediate errors during data retrieval (from OnGetAsync's exception handler)
            if (!ModelState.IsValid)
            {
                // If data loading failed, return the page to show errors.
                return Page();
            }
            // 1. Prepare CSV Content
            var builder = new StringBuilder();

            // Add Header Row
            builder.AppendLine("Date,Employee,Category,SubCategory,Amount,Commission Earned,Payment Type,Tracking Number,Description");

            // Add Data Rows
            foreach (var s in ReportData.SalesDetails)
            {
                // Simple CSV encoding: Ensure no commas in data fields are messing up the structure.
                // We enclose complex strings (like Description) in quotes.
                string descriptionClean = $"\"{s.Description?.Replace("\"", "\"\"")}\"";
                string trackingClean = $"\"{s.TrackingNumber?.Replace("\"", "\"\"")}\"";

                builder.AppendLine(
                    $"{s.Date.ToShortDateString()}," +
                    $"{s.EmployeeName}," +
                    $"{s.CategoryName}," +
                    $"{s.SubCategoryName}," +
                    $"{s.Amount}," +
                    $"{s.CommissionEarned}," +
                    $"{s.PaymentType}," +
                    $"{trackingClean}," +
                    $"{descriptionClean}"
                );
            }

            // 2. Convert to Byte Array
            var bytes = Encoding.UTF8.GetBytes(builder.ToString());

            // 3. Return FileResult
            _logger.Information("Exported sales report to CSV. Count: {Count}", ReportData.SalesDetails.Count);

            // Use File() to trigger a download
            return File(bytes, "text/csv", $"Sales_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
    }
}