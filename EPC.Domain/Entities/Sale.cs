namespace EPC.Domain.Entities
{
    public class Sale : Base
    {
        public int Id { get; set; }

        public string AppUserId { get; set; }
        public string EmployeeId { get; set; }
        public int SubCategoryId { get; set; }
        public SubCategory SubCategory { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public decimal CalculatedCommission { get; set; }

        public string PaymentType { get; set; } = default!; // "Cash" or "Card"
        public string? TrackingNumber { get; set; } // Optional: For Shipping, etc.
        public string? Description { get; set; } // Optional: For Miscellaneous sales
    }
}
