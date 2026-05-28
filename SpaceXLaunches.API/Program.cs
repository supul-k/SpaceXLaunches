using SpaceXLaunches.API.Extension;
using SpaceXLaunches.Application.Services;
using SpaceXLaunches.Domain.Interfaces;
using SpaceXLaunches.Infrastructure.ExternalServices;
using SpaceXLaunches.Infrastructure.Persistence;
using SpaceXLaunches.Infrastructure.Persistence.Repositories;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

string spaceXBaseUrl = builder.Configuration["SpaceX:BaseUrl"]
    ?? throw new InvalidOperationException("SpaceX:BaseUrl is missing from configuration.");

builder.Services.AddSingleton(new DbConnectionFactory(connectionString));

builder.Services.AddHttpClient<ISpaceXService, SpaceXApiService>((httpClient, serviceProvider) =>
{
    return new SpaceXApiService(httpClient, spaceXBaseUrl);
});

builder.Services.AddScoped<ILaunchRepository, LaunchRepository>();
builder.Services.AddScoped<LaunchService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
await EnsureSchema.EnsureSchemaAsync(connectionString);
app.Run();