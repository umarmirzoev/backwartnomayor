using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Реализует append-only хранение подписей и tenant-безопасный статус подписания.
/// </summary>
public sealed class SignatureRecordRepository : ISignatureRecordRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Инициализирует репозиторий общим scoped-контекстом.
    /// </summary>
    /// <param name="context">Контекст данных приложения.</param>
    public SignatureRecordRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(
        SignatureRecord signature,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(signature);
        await _context.SignatureRecords.AddAsync(signature, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SignatureRecord>> GetByDraftForLawyerAsync(
        Guid draftId,
        Guid lawyerId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(draftId, nameof(draftId));
        RepositoryGuards.EnsureNotEmpty(lawyerId, nameof(lawyerId));

        var query =
            from signature in _context.SignatureRecords.AsNoTracking()
            join draft in _context.Drafts.AsNoTracking()
                on signature.DraftId equals draft.Id
            join caseItem in _context.Cases.AsNoTracking()
                on draft.CaseId equals caseItem.Id
            where signature.DraftId == draftId && caseItem.LawyerId == lawyerId
            orderby signature.SignedAt, signature.Id
            select signature;

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SignatureRecord>> GetByDraftAsync(
        Guid draftId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(draftId, nameof(draftId));
        return await _context.SignatureRecords
            .AsNoTracking()
            .Where(signature => signature.DraftId == draftId)
            .OrderBy(signature => signature.SignedAt)
            .ThenBy(signature => signature.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsForSignerAsync(
        Guid draftId,
        PartyType signerType,
        Guid signerId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(draftId, nameof(draftId));
        RepositoryGuards.EnsureNotEmpty(signerId, nameof(signerId));

        return await _context.SignatureRecords.AnyAsync(
            signature => signature.DraftId == draftId &&
                         signature.SignerType == signerType &&
                         signature.SignerId == signerId,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CountByDraftAsync(
        Guid draftId,
        CancellationToken cancellationToken)
    {
        RepositoryGuards.EnsureNotEmpty(draftId, nameof(draftId));
        return await _context.SignatureRecords.CountAsync(
            signature => signature.DraftId == draftId,
            cancellationToken);
    }
}
