using Application.Interfaces.Services;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Authorization;

/// <summary>
/// Выполняет tenant-безопасную проверку доступа к документам непосредственно в SQL без предварительного раскрытия сущности.
/// Для юриста владение определяется Case.LawyerId, а для клиента — Case.ClientId; одинаковый false предотвращает IDOR и enumeration.
/// </summary>
public sealed class ResourceAuthorizationService : IResourceAuthorizationService
{
    private readonly AppDbContext _dbContext;

    /// <summary>Инициализирует сервис общим scoped-контекстом запроса.</summary>
    /// <param name="dbContext">Контекст доменных данных.</param>
    public ResourceAuthorizationService(AppDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    /// <summary>
    /// Проверяет доступ стороны к черновику одним SQL EXISTS через его родительское дело.
    /// </summary>
    /// <param name="draftId">Идентификатор черновика.</param>
    /// <param name="partyType">Доверенный тип стороны из JWT.</param>
    /// <param name="partyId">Доверенный идентификатор стороны из JWT.</param>
    /// <param name="cancellationToken">Токен отмены SQL-запроса.</param>
    /// <returns>Признак владения ресурсом.</returns>
    public Task<bool> CanAccessDraftAsync(
        Guid draftId,
        PartyType partyType,
        Guid partyId,
        CancellationToken cancellationToken)
    {
        if (draftId == Guid.Empty || partyId == Guid.Empty)
        {
            return Task.FromResult(false);
        }

        return partyType switch
        {
            PartyType.Lawyer => (
                from draft in _dbContext.Drafts.AsNoTracking()
                join caseEntity in _dbContext.Cases.AsNoTracking() on draft.CaseId equals caseEntity.Id
                where draft.Id == draftId && caseEntity.LawyerId == partyId
                select draft.Id).AnyAsync(cancellationToken),
            PartyType.Client => (
                from draft in _dbContext.Drafts.AsNoTracking()
                join caseEntity in _dbContext.Cases.AsNoTracking() on draft.CaseId equals caseEntity.Id
                where draft.Id == draftId && caseEntity.ClientId == partyId
                select draft.Id).AnyAsync(cancellationToken),
            _ => Task.FromResult(false)
        };
    }

    /// <summary>
    /// Проверяет доступ к immutable-версии через цепочку DocumentVersion → Draft → Case одним SQL EXISTS.
    /// </summary>
    /// <param name="documentVersionId">Идентификатор версии.</param>
    /// <param name="partyType">Доверенный тип стороны.</param>
    /// <param name="partyId">Доверенный идентификатор стороны.</param>
    /// <param name="cancellationToken">Токен отмены SQL-запроса.</param>
    /// <returns>Признак владения связанной версией.</returns>
    public Task<bool> CanAccessDocumentVersionAsync(
        Guid documentVersionId,
        PartyType partyType,
        Guid partyId,
        CancellationToken cancellationToken)
    {
        if (documentVersionId == Guid.Empty || partyId == Guid.Empty)
        {
            return Task.FromResult(false);
        }

        return partyType switch
        {
            PartyType.Lawyer => (
                from version in _dbContext.DocumentVersions.AsNoTracking()
                join draft in _dbContext.Drafts.AsNoTracking() on version.DraftId equals draft.Id
                join caseEntity in _dbContext.Cases.AsNoTracking() on draft.CaseId equals caseEntity.Id
                where version.Id == documentVersionId && caseEntity.LawyerId == partyId
                select version.Id).AnyAsync(cancellationToken),
            PartyType.Client => (
                from version in _dbContext.DocumentVersions.AsNoTracking()
                join draft in _dbContext.Drafts.AsNoTracking() on version.DraftId equals draft.Id
                join caseEntity in _dbContext.Cases.AsNoTracking() on draft.CaseId equals caseEntity.Id
                where version.Id == documentVersionId && caseEntity.ClientId == partyId
                select version.Id).AnyAsync(cancellationToken),
            _ => Task.FromResult(false)
        };
    }
}
