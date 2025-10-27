using System.ComponentModel.DataAnnotations;

namespace EPC.Application.Dtos
{
    public class SubCategoryCommand
    {
        // Used for Edit, null for Add
        public int Id { get; set; }

        [Required(ErrorMessage = "Parent Category is required.")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "SubCategory Name is required.")]
        [StringLength(100, ErrorMessage = "SubCategory Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Commission Rate is required.")]
        [Range(0.00, 1.00, ErrorMessage = "Rate must be between 0.00 and 1.00 (e.g., 0.05 for 5%).")]
        public decimal CommissionRate { get; set; }
    }
}
