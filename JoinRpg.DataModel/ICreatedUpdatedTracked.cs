using System;

namespace JoinRpg.DataModel
{
    public interface ICreatedUpdatedTracked
    {
        DateTime CreatedAt { get; }
        User CreatedBy { get; }

        DateTime UpdatedAt { get; }
        User UpdatedBy { get; }
    }

    public interface ICreatedUpdatedTrackedForEntity
    {
        DateTime CreatedAt { get; set; }
        User CreatedBy { get; set; }
        int CreatedById { get; set; }

        DateTime UpdatedAt { get; set; }
        User UpdatedBy { get; set; }
        int UpdatedById { get; set; }
    }

}
