using Application.Common.Models;
using Application.DTOs;
using Application.Interfaces.Services;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Infrastructure.Options;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Infrastructure.Export;

/// <summary>
/// Формирует DOCX и PDF из уже авторизованного расшифрованного текста без промежуточных файлов на диске.
/// Байты возвращаются Application-слою, поэтому внутренние пути и ключи объектного хранилища не раскрываются клиенту.
/// </summary>
public sealed class DocumentExportService : IDocumentExportService
{
    /// <summary>
    /// Инициализирует экспорт и явно применяет выбранную владельцем проекта лицензию QuestPDF.
    /// Неподдерживаемое значение останавливает PDF-генерацию до публикации приложения.
    /// </summary>
    /// <param name="options">Настройки фактически применимой лицензии QuestPDF.</param>
    public DocumentExportService(IOptions<DocumentExportOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!Enum.TryParse<LicenseType>(
                options.Value.QuestPdfLicense,
                ignoreCase: true,
                out var license))
        {
            throw new InvalidOperationException(
                "Лицензия QuestPDF должна иметь значение Community, Professional или Enterprise.");
        }

        QuestPDF.Settings.License = license;
    }

    /// <summary>
    /// Выбирает безопасный генератор по перечислению формата и создаёт документ полностью в памяти.
    /// </summary>
    /// <param name="draftId">Идентификатор документа для безопасного имени файла.</param>
    /// <param name="content">Авторизованный расшифрованный текст текущей версии.</param>
    /// <param name="format">DOCX или PDF.</param>
    /// <param name="cancellationToken">Токен отмены до и после CPU-операции.</param>
    /// <returns>Имя, MIME-тип и байты экспортированного файла.</returns>
    public Task<ServiceResult<ExportedDocumentDto>> ExportAsync(
        Guid draftId,
        string content,
        DocumentExportFormat format,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        if (draftId == Guid.Empty)
        {
            throw new ArgumentException("Идентификатор экспортируемого документа обязателен.", nameof(draftId));
        }

        var exported = format switch
        {
            DocumentExportFormat.Docx => CreateDocx(draftId, content),
            DocumentExportFormat.Pdf => CreatePdf(draftId, content),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Формат экспорта не поддерживается.")
        };
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ServiceResult<ExportedDocumentDto>.Success(exported));
    }

    /// <summary>Создаёт валидный WordprocessingDocument с отдельным абзацем для каждой строки текста.</summary>
    /// <param name="draftId">Идентификатор для имени файла.</param>
    /// <param name="content">Текст документа.</param>
    /// <returns>DOCX в памяти.</returns>
    private static ExportedDocumentDto CreateDocx(Guid draftId, string content)
    {
        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            var mainPart = document.AddMainDocumentPart();
            var body = new Body();
            foreach (var line in SplitLines(content))
            {
                body.AppendChild(
                    new Paragraph(
                        new Run(
                            new Text(line) { Space = SpaceProcessingModeValues.Preserve })));
            }

            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(body);
            mainPart.Document.Save();
        }

        return new ExportedDocumentDto(
            $"contract-{draftId:N}.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            stream.ToArray());
    }

    /// <summary>Создаёт PDF A4 с читаемыми полями и поддержкой многострочного текста.</summary>
    /// <param name="draftId">Идентификатор для имени файла.</param>
    /// <param name="content">Текст документа.</param>
    /// <returns>PDF в памяти.</returns>
    private static ExportedDocumentDto CreatePdf(Guid draftId, string content)
    {
        var lines = SplitLines(content);
        var bytes = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(style => style.FontSize(11));
                page.Content().Column(column =>
                {
                    column.Spacing(6);
                    foreach (var line in lines)
                    {
                        column.Item().Text(line);
                    }
                });
            });
        }).GeneratePdf();

        return new ExportedDocumentDto(
            $"contract-{draftId:N}.pdf",
            "application/pdf",
            bytes);
    }

    /// <summary>Нормализует переносы строк без изменения содержимого отдельных абзацев.</summary>
    /// <param name="content">Исходный текст.</param>
    /// <returns>Непустой набор строк.</returns>
    private static string[] SplitLines(string content)
    {
        var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        return lines.Length == 0 ? [string.Empty] : lines;
    }
}
