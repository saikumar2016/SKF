
using SKF.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<OpenAIService>();
builder.Services.AddSingleton(sp =>
    new DatasheetService(builder.Configuration.GetSection("Datasheets").Get<string[]>()));
builder.Services.AddSingleton(sp =>
    new CacheService(builder.Configuration["Redis:ConnectionString"]));

var app = builder.Build();
app.MapControllers();
app.Run();
