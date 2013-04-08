namespace Otter.Mvc.Infrastructure
{
    using AutoMapper;
    using Otter.Mvc.Domain;
    using Otter.Mvc.Models;

    public static class AutomapperConfiguration
    {
        public static void Configure()
        {
            Mapper.CreateMap<Article, ArticleReadModel>();
        }
    }
}