namespace Domain.Exceptions;

/// <summary>
/// Представляет базовое исключение для нарушений доменных правил и недопустимых переходов состояния.
/// Исключение не предназначено для технических ошибок инфраструктуры.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Инициализирует доменное исключение с понятным пользователю сообщением.
    /// </summary>
    /// <param name="message">Сообщение о нарушенном доменном правиле на русском языке.</param>
    public DomainException(string message)
        : base(message)
    {
    }
}
