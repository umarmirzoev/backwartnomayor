namespace Domain.Exceptions;

/// <summary>
/// Представляет ошибку проверки входных данных доменной сущности или объекта-значения.
/// Используется, когда переданные значения не позволяют создать или изменить корректное состояние.
/// </summary>
public sealed class DomainValidationException : DomainException
{
    /// <summary>
    /// Инициализирует исключение проверки доменных данных.
    /// </summary>
    /// <param name="message">Сообщение о нарушенном правиле валидации на русском языке.</param>
    public DomainValidationException(string message)
        : base(message)
    {
    }
}
