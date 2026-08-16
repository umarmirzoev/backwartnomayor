namespace Domain.Enums;

/// <summary>
/// Определяет полный жизненный цикл черновика и договора, включая состояния целевой версии продукта.
/// </summary>
public enum DocumentStatus
{
    /// <summary>Документ доступен для внутренних правок юриста.</summary>
    Draft,

    /// <summary>Документ ожидает согласования партнёром юридической фирмы.</summary>
    PendingFirmApproval,

    /// <summary>Документ отправлен клиенту через будущий клиентский портал.</summary>
    SentToClient,

    /// <summary>Клиент запросил изменения условий документа.</summary>
    RevisionsRequested,

    /// <summary>Клиент принял текущую редакцию документа.</summary>
    AcceptedByClient,

    /// <summary>Финальная редакция ожидает подписей сторон.</summary>
    AwaitingSignature,

    /// <summary>Документ подписан всеми необходимыми сторонами.</summary>
    Signed,

    /// <summary>Клиент отклонил документ или отказался от сделки.</summary>
    RejectedByClient,

    /// <summary>Клиент не ответил до установленного срока.</summary>
    Expired,

    /// <summary>Юрист отозвал документ из рассмотрения.</summary>
    RevokedByLawyer,

    /// <summary>Подписанный документ требует пересмотра из-за изменения законодательства.</summary>
    RequiresUpdate,

    /// <summary>Документ выведен из активной работы, но сохранён для отчётности.</summary>
    Archived,

    /// <summary>Документ помечен как удалённый в рамках жизненного цикла.</summary>
    Deleted
}
