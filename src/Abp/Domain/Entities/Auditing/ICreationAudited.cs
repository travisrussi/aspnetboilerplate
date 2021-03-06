namespace Abp.Domain.Entities.Auditing
{
    /// <summary>
    /// This interface is implemented by entities that is wanted to store creation informations (who and when created).
    /// Creation time and creator user are automatically set when saving <see cref="Entity"/> to database.
    /// </summary>
    public interface ICreationAudited : IHasCreationTime
    {
        /// <summary>
        /// Creator of this entity.
        /// </summary>
        long? CreatorUserId { get; set; }
    }
}