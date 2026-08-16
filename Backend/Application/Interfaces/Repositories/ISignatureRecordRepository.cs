using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Определяет append-only хранение Post-MVP записей электронной подписи.
/// </summary>
public interface ISignatureRecordRepository
{
    /// <summary>
    /// Добавляет юридически значимую запись подписи в текущую единицу работы.
    /// </summary>
    /// <param name="signature">Новая запись подписи.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Задача постановки записи на добавление.</returns>
    Task AddAsync(SignatureRecord signature, CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает подписи черновика после проверки принадлежности его дела юристу.
    /// </summary>
    /// <param name="draftId">Идентификатор черновика.</param>
    /// <param name="lawyerId">Идентификатор профиля владельца.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Записи подписей в хронологическом порядке.</returns>
    Task<IReadOnlyList<SignatureRecord>> GetByDraftForLawyerAsync(
        Guid draftId,
        Guid lawyerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает подписи черновика после отдельной успешной ресурсной авторизации юриста или клиента.
    /// Обработчик обязан проверить доступ до вызова этого метода.
    /// </summary>
    Task<IReadOnlyList<SignatureRecord>> GetByDraftAsync(
        Guid draftId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Проверяет, подписывала ли конкретная сторона данный черновик.
    /// </summary>
    /// <param name="draftId">Идентификатор черновика.</param>
    /// <param name="signerType">Тип подписанта.</param>
    /// <param name="signerId">Идентификатор подписанта.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns><see langword="true"/>, если подпись уже зафиксирована.</returns>
    Task<bool> ExistsForSignerAsync(
        Guid draftId,
        PartyType signerType,
        Guid signerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Подсчитывает подписи черновика для определения завершения процесса подписания.
    /// </summary>
    /// <param name="draftId">Идентификатор черновика.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Количество зафиксированных подписей.</returns>
    Task<int> CountByDraftAsync(Guid draftId, CancellationToken cancellationToken);
}
