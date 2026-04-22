using SearchHub.Api.AppStart.Extensions;
using SearchHub.Api.Configuration;
using SearchHub.Api.Services;

namespace SearchHub.Api.AppStart
{
    public class Startup
    {
        private WebApplicationBuilder _builder;

        public Startup(WebApplicationBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public void Initialize()
        {
            if (_builder.Environment.IsDevelopment())
            {
                _builder.Services.AddSwaggerGen();
            }
            else
            {
                _builder.Services.ConfigureCors();
            }

            InitConfigs();
            ConfigureServices();            

            _builder.Services.AddControllers();
        }

        private void InitConfigs()
        {            
            _builder.Services.Configure<SearchHubConfiguration>(_builder.Configuration.GetSection(nameof(SearchHubConfiguration)));            
        }

        private void ConfigureServices()
        {
            _builder.Services.AddHttpClient<ICrawlerService, CrawlerService>();
            _builder.Services.AddSingleton<ILuceneIndexService, LuceneIndexService>();
            _builder.Services.AddSingleton<IDataSerializer, ProtobufDataSerializer>();
            _builder.Services.AddSingleton<IIndexingService, IndexingService>();

            _builder.Services.AddSingleton<SearchHubConfiguration>(sp =>
                sp.GetRequiredService<IConfiguration>().GetSection(nameof(SearchHubConfiguration)).Get<SearchHubConfiguration>()
                ?? new SearchHubConfiguration { Sites = [] });
        }
    }
}
