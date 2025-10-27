namespace EPC.Domain.Entities
{
    public class Category : Base
    {
        public int Id { get; set; }
        public string? CategoryId { get; set; }
        public string Name { get; set; }
        public decimal? CommissionRate { get; set; }
        public List<SubCategory> SubCategories { get; set; } = new List<SubCategory>();
    }
}
