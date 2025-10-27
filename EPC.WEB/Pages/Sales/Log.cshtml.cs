using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EPC.Application.Services;
using EPC.Infrastructure.Data;
using EPC.Domain.Entities;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using EPC.Application.Services;
using EPC.Domain.Entities;
using EPC.Infrastructure.Data;

namespace EPC.WEB.Pages.Sales
{
    [Authorize]
    public class LogModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly SaleService _saleService;

        public LogModel(AppDbContext context, SaleService saleService)
        {
            _context = context;
            _saleService = saleService;
        }

        public List<SubCategory> SubCategories { get; set; }

        [BindProperty]
        public int SubCategoryId { get; set; }

        [BindProperty]
        public decimal Amount { get; set; }

        public async Task OnGetAsync()
        {
            SubCategories = await _context.SubCategories.Include(sc => sc.Category).ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var employee = await _context.Users.FirstOrDefaultAsync(e => e.Email == User.Identity.Name);

            if (employee != null)
                await _saleService.AddSaleAsync(employee.Id, SubCategoryId, Amount);

            return RedirectToPage("/Sales/History");
        }
    }
}
