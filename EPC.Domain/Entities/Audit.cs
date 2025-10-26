namespace EPC.Domain.Entities
{
    public class Audit
    {
        public Guid Id { get; set; }
        public string Action { get; set; } = "";
        public string? PerformedBy { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
