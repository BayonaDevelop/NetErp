namespace Identity.Core.Entities;

public partial class Role
{
  public int Id { get; set; }

  public required string Name { get; set; }

  public required string NormalizedName { get; set; }

  public virtual ICollection<User> Users { get; set; } = [];
}
