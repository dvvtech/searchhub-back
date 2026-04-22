using SearchHub.Api.AppStart;
using SearchHub.Api.AppStart.Extensions;
using SearchHub.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var startup = new Startup(builder);
startup.Initialize();

var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.ApplyCors();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

var indexingService = app.Services.GetRequiredService<IIndexingService>();
_ = Task.Run(() => indexingService.ReindexAllAsync());

app.Run();

//public partial class Program { }
