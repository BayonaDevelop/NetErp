namespace Identity.Core.Entities;

public partial class LoginAttempt
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Email { get; set; } = null!;

    public bool Success { get; set; }

    public DateTime AttemptedAt { get; set; }

    public string? IpAddress { get; set; }

    public virtual User? User { get; set; }
}
