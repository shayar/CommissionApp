using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using EPC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using EPC.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using EPC.Infrastructure.Identity;
using Serilog;

namespace EPC.WEB.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class ReportsModel : PageModel
    {
        private readonly AppDbContext _context;

        public ReportsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<Sale> Sales { get; set; } = new();
        public Dictionary<string, decimal> TotalsByCategory { get; set; } = new();
        public List<AppUser> Employees { get; set; } = new();

        [BindProperty(SupportsGet = true)] public string? AppUserId { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? StartDate { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? EndDate { get; set; }
        [BindProperty(SupportsGet = true)] public string? Search { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var query = _context.Sales
                    .Include(s => s.SubCategory)
                        .ThenInclude(sc => sc.Category)
                    .AsQueryable();

                Employees = await _context.Users.ToListAsync();

                if (!string.IsNullOrEmpty(AppUserId))
                    query = query.Where(s => s.AppUserId == AppUserId);

                if (StartDate.HasValue)
                    query = query.Where(s => s.Date >= StartDate.Value);

                if (EndDate.HasValue)
                    query = query.Where(s => s.Date <= EndDate.Value);

                if (!string.IsNullOrWhiteSpace(Search))
                    query = query.Where(s =>
                        s.SubCategory.Name.Contains(Search) ||
                        s.SubCategory.Category.Name.Contains(Search));

                Sales = await query.ToListAsync();

                TotalsByCategory = Sales
                    .GroupBy(s => s.SubCategory.Category.Name)
                    .ToDictionary(g => g.Key, g => g.Sum(s => s.Amount));

                Log.Information("Admin viewed sales report with filters. Employee: {Employee}, Start: {Start}, End: {End}, Search: {Search}", AppUserId, StartDate, EndDate, Search);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load sales reports");
                ModelState.AddModelError(string.Empty, "Something went wrong while loading the report. Please try again.");
            }
        }
    }
}
