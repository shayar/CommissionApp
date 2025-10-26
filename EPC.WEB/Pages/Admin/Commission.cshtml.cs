using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EPC.Infrastructure.Data;
using EPC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using EPC.Domain.Entities;
using EPC.Infrastructure.Data;

namespace EPC.WEB.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CommissionModel : PageModel
    {
        private readonly AppDbContext _context;

        public CommissionModel(AppDbContext context) => _context = context;

        public List<SubCategory> SubCategories { get; set; }

        [BindProperty]
        public int SubCategoryId { get; set; }

        [BindProperty]
        public decimal CommissionRate { get; set; }

        public async Task OnGetAsync()
        {
            SubCategories = await _context.SubCategories.Include(sc => sc.Category).ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var sub = await _context.SubCategories.FindAsync(SubCategoryId);
            if (sub == null) return Page();
            sub.CommissionRate = CommissionRate;
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}
