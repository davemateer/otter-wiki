namespace Otter.Infrastructure
{
    using AutoMapper;
    using Otter.Domain;
    using Otter.Models;

    public static class AutomapperConfiguration
    {
        public static void Configure()
        {
            Mapper.CreateMap<Article, ArticleReadModel>();
            Mapper.CreateMap<Article, ArticleEditModel>();
            Mapper.CreateMap<Article, ArticleHistoryModel>();

            Mapper.AssertConfigurationIsValid();
        }
    }
}