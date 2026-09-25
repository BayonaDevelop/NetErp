using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Data;

public partial class SqlServerDbContext : DbContext
{
  public SqlServerDbContext() { }

  public SqlServerDbContext(DbContextOptions<SqlServerDbContext> options) : base(options) { }

  public virtual DbSet<LoginAttempt> LoginAttempts { get; set; }

  public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

  public virtual DbSet<Role> Roles { get; set; }

  public virtual DbSet<User> Users { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<LoginAttempt>(entity =>
    {
      entity.HasKey(e => e.Id).HasName("PK__LoginAtt__3214EC07D81C9D68");

      entity.ToTable("LoginAttempts", "sec");

      entity.HasIndex(e => e.Email, "IX_LoginAttempts_Email");

      entity.Property(e => e.AttemptedAt).HasDefaultValueSql("(sysutcdatetime())");
      entity.Property(e => e.Email).HasMaxLength(256);
      entity.Property(e => e.IpAddress).HasMaxLength(64);

      entity.HasOne(d => d.User).WithMany(p => p.LoginAttempts)
          .HasForeignKey(d => d.UserId)
          .OnDelete(DeleteBehavior.SetNull)
          .HasConstraintName("FK_LoginAttempts_Users");
    });

    modelBuilder.Entity<RefreshToken>(entity =>
    {
      entity.HasKey(e => e.Id).HasName("PK__RefreshT__3214EC07553D2B72");

      entity.ToTable("RefreshTokens", "sec");

      entity.HasIndex(e => e.UserId, "IX_RefreshTokens_UserId");

      entity.HasIndex(e => e.TokenHash, "UQ_RefreshTokens_TokenHash").IsUnique();

      entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
      entity.Property(e => e.CreatedByIp).HasMaxLength(64);
      entity.Property(e => e.TokenHash).HasMaxLength(256);

      entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens)
          .HasForeignKey(d => d.UserId)
          .HasConstraintName("FK_RefreshTokens_Users");
    });

    modelBuilder.Entity<Role>(entity =>
    {
      entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC0721FE0068");

      entity.ToTable("Roles", "sec");

      entity.HasIndex(e => e.Name, "UQ_Roles_Name").IsUnique();

      entity.Property(e => e.Name).HasMaxLength(100);
    });

    modelBuilder.Entity<User>(entity =>
    {
      entity.HasKey(e => e.Id).HasName("PK__Users__3214EC07EA587A22");

      entity.ToTable("Users", "sec");

      entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

      entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF__Users__CreatedAt__10216507");
      entity.Property(e => e.Email).HasMaxLength(256);
      entity.Property(e => e.IsActive).HasDefaultValue(true, "DF__Users__IsActive__0F2D40CE");

      entity.HasMany(d => d.Roles).WithMany(p => p.Users)
          .UsingEntity<Dictionary<string, object>>(
              "UserRole",
              r => r.HasOne<Role>().WithMany()
                  .HasForeignKey("RoleId")
                  .HasConstraintName("FK_UserRoles_Roles"),
              l => l.HasOne<User>().WithMany()
                  .HasForeignKey("UserId")
                  .HasConstraintName("FK_UserRoles_Users"),
              j =>
              {
                j.HasKey("UserId", "RoleId");
                j.ToTable("UserRoles", "sec");
              });
    });

    OnModelCreatingPartial(modelBuilder);
  }

  partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
