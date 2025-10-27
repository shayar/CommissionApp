using EPC.Domain.Entities;

namespace EPC.Infrastructure.Repos
{
    public interface ICategoryRepository
    {
        // Category Operations
        Task<List<Category>> GetAllCategoriesWithSubCategoriesAsync();
        Task<bool> CategoryNameExistsAsync(string name, int excludeId = 0);
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<Category> AddCategoryAsync(Category category);
        Task UpdateCategoryAsync(Category category);

        // SubCategory Operations
        Task<SubCategory?> GetSubCategoryByIdAsync(int id);
        Task<bool> SubCategoryNameExistsAsync(string name, int categoryId, int excludeId = 0);
        Task<SubCategory> AddSubCategoryAsync(SubCategory subCategory);
        Task UpdateSubCategoryAsync(SubCategory subCategory);
    }
}
