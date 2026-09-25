namespace Identity.Core.Entities;

public partial class User
{
    public int Id { get; set; }

    public long CompanyId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<LoginAttempt> LoginAttempts { get; set; } = [];

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    public virtual ICollection<Role> Roles { get; set; } = [];
}
