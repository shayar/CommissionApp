using EPC.Domain.Entities;
using EPC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EPC.Infrastructure.Repos.Impl
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger _logger;
        public CategoryRepository(AppDbContext context, ILogger logger)
        {
            _context = context;
            _logger = logger.ForContext<CategoryRepository>();
        }

        public async Task<List<Category>> GetAllCategoriesWithSubCategoriesAsync()
        {
            return await _context.Categories
                .Include(c => c.SubCategories.OrderBy(sc => sc.Name))
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> CategoryNameExistsAsync(string name, int excludeId = 0)
        {
            return await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == name.ToLower() && c.Id != excludeId);
        }

        public async Task<Category> AddCategoryAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        // --- SubCategory Operations ---

        public async Task<SubCategory?> GetSubCategoryByIdAsync(int id)
        {
            return await _context.SubCategories.FindAsync(id);
        }

        public async Task<bool> SubCategoryNameExistsAsync(string name, int categoryId, int excludeId = 0)
        {
            return await _context.SubCategories
                .AnyAsync(sc =>
                    sc.Name.ToLower() == name.ToLower() &&
                    sc.CategoryId == categoryId &&
                    sc.Id != excludeId);
        }

        public async Task<SubCategory> AddSubCategoryAsync(SubCategory subCategory)
        {
            _context.SubCategories.Add(subCategory);
            await _context.SaveChangesAsync();
            return subCategory;
        }

        public async Task UpdateSubCategoryAsync(SubCategory subCategory)
        {
            _context.SubCategories.Update(subCategory);
            await _context.SaveChangesAsync();
        }
    }
}
