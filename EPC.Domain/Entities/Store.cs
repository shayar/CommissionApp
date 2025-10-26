namespace EPC.Domain.Entities
{
    public class Store : Base
    {
        public Guid Id { get; set; }
        public string? StoreId { get; set; }
        public string Name { get; set; }
        public string StreetAddress1 { get; set; }
        public string? StreetAddress2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string PhoneNumber { get; set; }
        public string Country { get; set; } = "US";
    }
}
