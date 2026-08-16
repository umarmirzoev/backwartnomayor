namespace Domain.Enums;

/// <summary>
/// Определяет состояние обработки запроса на полное удаление данных.
/// </summary>
public enum DataDeletionStatus
{
    /// <summary>Запрос зарегистрирован и ожидает исполнения.</summary>
    Pending,

    /// <summary>Требуемые данные были успешно уничтожены или анонимизированы.</summary>
    Completed,

    /// <summary>Запрос был отклонён по допустимому бизнес-основанию.</summary>
    Rejected
}
