using SearchHub.Api.Configuration;
using SearchHub.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SearchHubConfiguration>(
    builder.Configuration.GetSection("SearchHub"));

builder.Services.AddSingleton<SearchHubConfiguration>(sp =>
    sp.GetRequiredService<IConfiguration>().GetSection("SearchHub").Get<SearchHubConfiguration>()
    ?? new SearchHubConfiguration { Sites = [] });

builder.Services.AddHttpClient<ICrawlerService, CrawlerService>();
builder.Services.AddSingleton<ILuceneIndexService, LuceneIndexService>();
builder.Services.AddSingleton<IIndexingService, IndexingService>();

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

var indexingService = app.Services.GetRequiredService<IIndexingService>();
_ = Task.Run(() => indexingService.ReindexAllAsync());

app.Run();

public partial class Program { }
