namespace WebApi.Security;

/// <summary>
/// Предоставляет AsyncLocal-признак доверенного фонового исполнения внутри текущей асинхронной цепочки.
/// Значение устанавливается только зарегистрированным BackgroundService и недоступно из HTTP-запроса.
/// </summary>
public sealed class SystemExecutionContext
{
    private static readonly AsyncLocal<int> Depth = new();

    /// <summary>Получает признак активной доверенной системной области.</summary>
    public bool IsSystem => Depth.Value > 0;

    /// <summary>
    /// Открывает вложенную системную область для вызова CQRS-команд фоновым работником.
    /// Возвращаемый объект обязан быть освобождён после завершения операции.
    /// </summary>
    /// <returns>Область, восстанавливающая предыдущее состояние при Dispose.</returns>
    public IDisposable Enter()
    {
        Depth.Value++;
        return new Scope();
    }

    /// <summary>Восстанавливает глубину системного контекста после завершения фоновой операции.</summary>
    private sealed class Scope : IDisposable
    {
        private bool _disposed;

        /// <summary>Закрывает ровно одну системную область и защищает от повторного освобождения.</summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Depth.Value = Math.Max(0, Depth.Value - 1);
            _disposed = true;
        }
    }
}
