namespace WebApi.Middleware;

/// <summary>Предоставляет типобезопасное подключение глобальной обработки исключений в начале HTTP-конвейера.</summary>
public static class GlobalExceptionHandlingExtensions
{
    /// <summary>
    /// Добавляет GlobalExceptionHandlingMiddleware так, чтобы он охватывал все последующие компоненты и контроллеры.
    /// </summary>
    /// <param name="app">Построитель HTTP-конвейера.</param>
    /// <returns>Тот же построитель для последовательной настройки.</returns>
    public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}
