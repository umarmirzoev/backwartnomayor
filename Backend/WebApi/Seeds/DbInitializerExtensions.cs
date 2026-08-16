namespace WebApi.Seeds;

/// <summary>Предоставляет единый вызов инициализации базы после построения маршрутов WebAPI.</summary>
public static class DbInitializerExtensions
{
    /// <summary>
    /// Создаёт изолированную область, выполняет DbInitializer и освобождает scoped-зависимости до начала приёма трафика.
    /// </summary>
    /// <param name="app">Построенное WebAPI-приложение.</param>
    /// <param name="cancellationToken">Токен отмены запуска.</param>
    /// <returns>Задача инициализации базы.</returns>
    public static async Task SeedDatabaseAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);
        await using var scope = app.Services.CreateAsyncScope();
        var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
        await initializer.SeedAsync(cancellationToken);
    }
}
