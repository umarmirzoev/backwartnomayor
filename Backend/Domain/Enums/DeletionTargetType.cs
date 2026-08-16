namespace Domain.Enums;

/// <summary>
/// Определяет тип доменного объекта, данные которого требуется полностью удалить.
/// </summary>
public enum DeletionTargetType
{
    /// <summary>Удалению или анонимизации подлежат данные клиента.</summary>
    Client,

    /// <summary>Удалению подлежат данные дела.</summary>
    Case,

    /// <summary>Удалению подлежат черновик и его содержимое.</summary>
    Draft
}
