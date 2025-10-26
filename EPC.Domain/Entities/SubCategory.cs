namespace EPC.Domain.Entities
{
    public class SubCategory : Base
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public decimal CommissionRate { get; set; } // 0.1 for 10%
    }
}
