using EPC.Domain.Entities;
using EPC.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace EPC.WEB.Pages.Sales;

[Authorize(Roles = "Employee")]
public class HistoryModel : PageModel
{
    private readonly AppDbContext _context;
    public List<Sale> Sales { get; set; }

    public HistoryModel(AppDbContext context) => _context = context;

    public async Task OnGetAsync()
    {
        var user = await _context.Users.FirstOrDefaultAsync(e => e.Email == User.Identity.Name);
        if (user == null)
        {
            Sales = new List<Sale>();
            return;
        }

        Sales = await _context.Sales
            .Where(s => s.EmployeeId == user.Id)
            .Include(s => s.SubCategory)
            .ThenInclude(sc => sc.Category)
            .ToListAsync();
    }
}
