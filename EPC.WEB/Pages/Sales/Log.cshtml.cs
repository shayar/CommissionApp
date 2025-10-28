using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EPC.Infrastructure.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using EPC.Application.Services;

namespace EPC.WEB.Pages.Sales
{
    [Authorize]
    public class LogModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ISaleService _saleService;
        private readonly Serilog.ILogger _logger;

        public LogModel(AppDbContext context, ISaleService saleService, Serilog.ILogger logger)
        {
            _context = context;
            _saleService = saleService;
            _logger = logger.ForContext<LogModel>();
        }

        // Dropdown ViewModel
        public List<CommissionableItemViewModel> CommissionableItems { get; set; } = new List<CommissionableItemViewModel>();

        // Input Model
        [BindProperty, Required(ErrorMessage = "A commissionable item must be selected.")]
        public string SelectedItemId { get; set; } = default!;

        [BindProperty, Required(ErrorMessage = "Sale amount is required.")]
        [Range(0.01, 999999.99, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }

        // --- NEW INPUTS ---
        [BindProperty, Required(ErrorMessage = "Payment type is required.")]
        public string PaymentType { get; set; } = default!;

        [BindProperty]
        public string? TrackingNumber { get; set; }

        [BindProperty]
        public string? Description { get; set; }
        // --- END NEW INPUTS ---

        public class CommissionableItemViewModel
        {
            public string Id { get; set; } = default!;
            public string DisplayName { get; set; } = default!;
        }

        public async Task OnGetAsync()
        {
            await LoadCommissionableItems();
        }

        private async Task LoadCommissionableItems()
        {
            var categories = await _context.Categories
                .Include(c => c.SubCategories)
                .OrderBy(c => c.Name)
                .ToListAsync();

            CommissionableItems.Clear();

            foreach (var cat in categories)
            {
                if (!cat.SubCategories.Any() && cat.CommissionRate.HasValue)
                {
                    CommissionableItems.Add(new CommissionableItemViewModel
                    {
                        Id = $"C_{cat.Id}",
                        DisplayName = $"{cat.Name} ({cat.CommissionRate.Value:P0})"
                    });
                }

                foreach (var sub in cat.SubCategories.OrderBy(sc => sc.Name))
                {
                    CommissionableItems.Add(new CommissionableItemViewModel
                    {
                        Id = $"SC_{sub.Id}",
                        DisplayName = $"{cat.Name} - {sub.Name} ({sub.CommissionRate:P0})"
                    });
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCommissionableItems();
                return Page();
            }

            var appUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(appUserId))
            {
                ModelState.AddModelError(string.Empty, "User identity could not be verified. Please log in again.");
                await LoadCommissionableItems();
                return Page();
            }

            var parts = SelectedItemId.Split('_');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int itemId))
            {
                ModelState.AddModelError(nameof(SelectedItemId), "Invalid commission item selected.");
                await LoadCommissionableItems();
                return Page();
            }

            var isSubCategory = parts[0] == "SC";

            try
            {
                // Call the Application Layer Interface method
                await _saleService.LogSaleAsync(
                    appUserId,
                    itemId,
                    Amount,
                    isSubCategory,
                    PaymentType,        // NEW
                    TrackingNumber,     // NEW
                    Description         // NEW
                );

                TempData["SuccessMessage"] = $"Sale of {Amount:C2} logged successfully.";
                return RedirectToPage("/Sales/Log");
            }
            // Catch exceptions thrown by the Application Service (business rule violations/KeyNotFound)
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to log sale.");
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            await LoadCommissionableItems();
            return Page();
        }
    }
}