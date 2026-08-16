using Domain.Entities;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет хранение упорядоченного состава пунктов каждого шаблона.
/// </summary>
public interface ITemplateClauseBlockRepository : IBaseRepository<TemplateClauseBlock>
{
    /// <summary>
    /// Возвращает связи шаблона в доменном порядке.
    /// </summary>
    /// <param name="templateId">Идентификатор шаблона.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Упорядоченный список связей.</returns>
    Task<IReadOnlyList<TemplateClauseBlock>> GetByTemplateAsync(
        Guid templateId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает конкретную связь шаблона и блока.
    /// </summary>
    /// <param name="templateId">Идентификатор шаблона.</param>
    /// <param name="clauseBlockId">Идентификатор блока.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Связь или <see langword="null"/>.</returns>
    Task<TemplateClauseBlock?> GetByTemplateAndClauseBlockAsync(
        Guid templateId,
        Guid clauseBlockId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Проверяет, прикреплён ли блок к шаблону.
    /// </summary>
    /// <param name="templateId">Идентификатор шаблона.</param>
    /// <param name="clauseBlockId">Идентификатор блока.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns><see langword="true"/>, если связь существует.</returns>
    Task<bool> ExistsAsync(
        Guid templateId,
        Guid clauseBlockId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Проверяет занятость позиции другим блоком внутри шаблона.
    /// </summary>
    /// <param name="templateId">Идентификатор шаблона.</param>
    /// <param name="order">Проверяемая позиция.</param>
    /// <param name="excludedLinkId">Редактируемая связь, которую следует исключить.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns><see langword="true"/>, если позиция занята.</returns>
    Task<bool> IsOrderOccupiedAsync(
        Guid templateId,
        int order,
        Guid? excludedLinkId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает активные блоки шаблона одним SQL-запросом без N+1.
    /// </summary>
    /// <param name="templateId">Идентификатор шаблона.</param>
    /// <param name="defaultOnly">Признак выборки только пунктов по умолчанию.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Активные блоки в порядке шаблона.</returns>
    Task<IReadOnlyList<ClauseBlock>> GetClauseBlocksByTemplateAsync(
        Guid templateId,
        bool defaultOnly,
        CancellationToken cancellationToken);
}
