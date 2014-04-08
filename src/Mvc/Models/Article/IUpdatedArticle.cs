namespace Otter.Models
{
    public interface IUpdatedArticle
    {
        string UpdatedBy { get; }

        string UpdatedByDisplayName { get; set; }
    }
}
