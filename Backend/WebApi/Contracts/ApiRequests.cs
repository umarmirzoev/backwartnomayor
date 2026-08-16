using Application.DTOs;
using Application.Interfaces.Services;

namespace WebApi.Contracts;

/// <summary>Представляет HTTP-тело повторной ИИ-генерации без дублирования идентификатора маршрута.</summary>
/// <param name="Instructions">Указания юриста для новой редакции.</param>
/// <param name="ChangeSummary">Обязательное описание отличий immutable-версии.</param>
public sealed record RegenerateDraftRequest(string Instructions, string ChangeSummary);

/// <summary>Представляет будущий срок ответа клиента для отправки или утверждения документа.</summary>
/// <param name="DueRespondByDate">Крайний срок ответа в UTC.</param>
public sealed record DraftDueDateRequest(DateTimeOffset DueRespondByDate);

/// <summary>Представляет выбранный формат экспорта текущей версии документа.</summary>
/// <param name="Format">DOCX или PDF.</param>
public sealed record ExportDocumentRequest(DocumentExportFormat Format);

/// <summary>Представляет доверенный результат мониторинга и набор затронутых дел.</summary>
/// <param name="Data">Содержимое законодательного уведомления.</param>
/// <param name="CaseIds">Уникальные идентификаторы затронутых дел.</param>
public sealed record IngestLegislationAlertRequest(
    CreateLegislationAlertDto Data,
    IReadOnlyCollection<Guid> CaseIds);

/// <summary>Представляет текст входящего договора для одноразового ИИ-анализа.</summary>
/// <param name="Content">Проверяемый текст без сохранения в агрегат Draft.</param>
public sealed record ReviewIncomingDocumentRequest(string Content);

/// <summary>Представляет законодательное основание перевода подписанного документа в RequiresUpdate.</summary>
/// <param name="LegislationAlertId">Идентификатор append-only уведомления.</param>
public sealed record MarkDraftRequiresUpdateRequest(Guid LegislationAlertId);
