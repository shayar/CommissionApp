using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EPC.Infrastructure.Data;
using EPC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using EPC.Domain.Entities;
using EPC.Infrastructure.Data;

namespace EPC.WEB.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CategoriesModel : PageModel
    {
        private readonly AppDbContext _context;
        [BindProperty] public Category NewCategory { get; set; }
        public List<Category> Categories { get; set; }

        public CategoriesModel(AppDbContext context) => _context = context;

        public async Task OnGetAsync() =>
            Categories = await _context.Categories.Include(c => c.SubCategories).ToListAsync();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            _context.Categories.Add(NewCategory);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}
