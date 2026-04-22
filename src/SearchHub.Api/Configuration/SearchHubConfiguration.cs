
namespace SearchHub.Api.Configuration
{
    public class SearchHubConfiguration
    {
        public required List<SiteConfiguration> Sites {  get; init; }
    }

    public class SiteConfiguration
    {        
        public int Id { get; init; }

        public required string Name { get; init; }
     
        public required string Url { get; init; }
    }
}
