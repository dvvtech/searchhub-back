
namespace SearchHub.Api.Configuration
{
    public class SearchHubConfiguration
    {
        public List<SiteConfiguration> Sites {  get; init; }
    }

    public class SiteConfiguration
    {        
        public int Id { get; init; }

        public string Name { get; init; }
     
        public string Url { get; init; }
    }
}
