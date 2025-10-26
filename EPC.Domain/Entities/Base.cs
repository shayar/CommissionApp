namespace EPC.Domain.Entities
{
    public abstract class Base
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
