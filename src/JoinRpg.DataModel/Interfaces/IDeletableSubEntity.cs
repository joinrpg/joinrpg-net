namespace JoinRpg.DataModel;

public interface IDeletableSubEntity
{
    bool CanBePermanentlyDeleted { get; }
    bool IsActive { get; set; }
}
