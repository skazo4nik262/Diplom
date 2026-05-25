namespace CatalogService.Data.Entities;

public class UserTasteEntity
{
    public Guid UserId { get; set; }
    public float[] Embedding { get; set; } = [];
    public DateTime UpdatedAt { get; set; }

    public UserEntity User { get; set; } = null!;
}
