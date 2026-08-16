using Application;
using Infrastructure;
using WebApi;
using WebApi.Middleware;
using WebApi.Seeds;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddWebApiServices(builder.Configuration);

var app = builder.Build();

app.UseGlobalExceptionMiddleware();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ShartnomaYor API v1"));
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("FrontendClients");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.SeedDatabaseAsync();
await app.RunAsync();

/// <summary>
/// Предоставляет публичный маркер точки входа для интеграционного WebApplicationFactory без переноса Composition Root.
/// </summary>
public partial class Program;
