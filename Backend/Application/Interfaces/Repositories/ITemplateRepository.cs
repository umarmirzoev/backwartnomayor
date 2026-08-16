using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет операции хранения каталога шаблонов договоров.
/// </summary>
public interface ITemplateRepository : IBaseRepository<Template>
{
    /// <summary>
    /// Возвращает активный шаблон, допустимый для создания нового черновика.
    /// </summary>
    /// <param name="templateId">Идентификатор шаблона.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Активный шаблон или <see langword="null"/>.</returns>
    Task<Template?> GetActiveByIdAsync(Guid templateId, CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает страницу активных шаблонов с необязательным фильтром языка.
    /// </summary>
    /// <param name="language">Язык или отсутствие языкового фильтра.</param>
    /// <param name="skip">Количество пропускаемых строк.</param>
    /// <param name="take">Максимальное количество строк.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Страница шаблонов, упорядоченная по имени.</returns>
    Task<IReadOnlyList<Template>> GetActivePageAsync(
        TemplateLanguage? language,
        int skip,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// Подсчитывает активные шаблоны с учётом языкового фильтра.
    /// </summary>
    /// <param name="language">Язык или отсутствие языкового фильтра.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Количество доступных шаблонов.</returns>
    Task<int> CountActiveAsync(
        TemplateLanguage? language,
        CancellationToken cancellationToken);
}
