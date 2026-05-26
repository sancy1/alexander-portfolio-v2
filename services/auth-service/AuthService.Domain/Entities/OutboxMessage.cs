using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.Domain.Entities;

[Table("OutboxMessages")]
public class OutboxMessage
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string RoutingKey { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string Broker { get; set; } = string.Empty;
    
    public string Payload { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ProcessedAt { get; set; }
    
    public int RetryCount { get; set; } = 0;
    
    public string? Error { get; set; }
    
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public bool IsProcessed { get; private set; }
}