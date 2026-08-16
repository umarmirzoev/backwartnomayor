using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Представляет неизменяемый снимок содержимого договора.
/// Сам текст хранится в объектном хранилище, а сущность содержит только ключ и метаданные версии.
/// После создания ни одно поле версии не изменяется.
/// </summary>
public sealed class DocumentVersion : BaseEntity
{
    /// <summary>
    /// Инициализирует версию документа при материализации сохранённых данных ORM.
    /// </summary>
    private DocumentVersion()
    {
    }

    /// <summary>
    /// Создаёт неизменяемую версию через агрегат <see cref="Draft"/>.
    /// </summary>
    /// <param name="draftId">Идентификатор родительского черновика.</param>
    /// <param name="versionNumber">Положительный номер версии.</param>
    /// <param name="contentStorageKey">Ключ содержимого в S3-совместимом хранилище.</param>
    /// <param name="changeSummary">Описание изменений относительно предыдущей версии.</param>
    /// <param name="source">Источник создания версии.</param>
    /// <param name="createdByLawyerId">Идентификатор профиля юриста-создателя.</param>
    /// <param name="createdAt">Момент создания версии в UTC.</param>
    internal DocumentVersion(
        Guid draftId,
        int versionNumber,
        string contentStorageKey,
        string? changeSummary,
        DocumentVersionSource source,
        Guid createdByLawyerId,
        DateTimeOffset createdAt)
        : base(Guid.NewGuid())
    {
        DraftId = Guard.AgainstEmpty(draftId, "идентификатор черновика");
        VersionNumber = Guard.AgainstNonPositive(versionNumber, "номер версии");
        ContentStorageKey = Guard.RequiredText(contentStorageKey, "ключ содержимого", 500);
        Source = Guard.AgainstInvalidEnum(source, "источник версии");
        CreatedByLawyerId = Guard.AgainstEmpty(createdByLawyerId, "идентификатор юриста-создателя");
        CreatedAt = Guard.AgainstDefault(createdAt, "дата создания версии");

        ValidateSourceAndSummary(versionNumber, source, changeSummary);
        ChangeSummary = source == DocumentVersionSource.AiRegenerated
            ? Guard.RequiredText(changeSummary, "описание изменений", 1000)
            : Guard.OptionalText(changeSummary, "описание изменений", 1000);
    }

    /// <summary>Получает идентификатор родительского черновика.</summary>
    public Guid DraftId { get; private set; }

    /// <summary>Получает монотонно возрастающий номер версии.</summary>
    public int VersionNumber { get; private set; }

    /// <summary>Получает ключ содержимого в объектном хранилище.</summary>
    public string ContentStorageKey { get; private set; } = string.Empty;

    /// <summary>Получает описание отличий от предыдущей версии.</summary>
    public string? ChangeSummary { get; private set; }

    /// <summary>Получает источник создания версии.</summary>
    public DocumentVersionSource Source { get; private set; }

    /// <summary>Получает идентификатор профиля юриста, создавшего версию.</summary>
    public Guid CreatedByLawyerId { get; private set; }

    /// <summary>Получает момент создания неизменяемой версии.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Проверяет согласованность номера версии, источника и описания изменений.
    /// </summary>
    /// <param name="versionNumber">Номер версии.</param>
    /// <param name="source">Источник версии.</param>
    /// <param name="changeSummary">Описание изменений.</param>
    private static void ValidateSourceAndSummary(
        int versionNumber,
        DocumentVersionSource source,
        string? changeSummary)
    {
        if (versionNumber == 1)
        {
            Guard.Against(
                source != DocumentVersionSource.AiGenerated,
                "Первая версия документа должна иметь источник AiGenerated.");
            Guard.Against(
                !string.IsNullOrWhiteSpace(changeSummary),
                "Первая версия документа не должна содержать описание изменений.");
            return;
        }

        Guard.Against(
            source == DocumentVersionSource.AiGenerated,
            "Источник AiGenerated разрешён только для первой версии документа.");

        Guard.Against(
            source == DocumentVersionSource.AiRegenerated && string.IsNullOrWhiteSpace(changeSummary),
            "Повторно сгенерированная версия должна содержать описание изменений.");
    }
}
