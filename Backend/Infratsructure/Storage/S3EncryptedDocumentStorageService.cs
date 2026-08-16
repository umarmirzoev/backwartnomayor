using System.Security.Cryptography;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Application.Common.Models;
using Application.Interfaces.Services;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

/// <summary>
/// Хранит содержимое документов в приватном S3-совместимом контейнере с дополнительным AES-256-GCM-шифрованием.
/// В PostgreSQL попадает только непрозрачный ключ объекта, а подмена ciphertext обнаруживается аутентификационным тегом.
/// </summary>
public sealed class S3EncryptedDocumentStorageService : IDocumentStorageService
{
    private const byte EnvelopeVersion = 1;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly IAmazonS3 _s3Client;
    private readonly DocumentStorageOptions _options;
    private readonly byte[] _encryptionKey;
    private readonly ILogger<S3EncryptedDocumentStorageService> _logger;

    /// <summary>
    /// Инициализирует S3-адаптер и проверяет Base64-ключ AES-256 до обработки первого документа.
    /// </summary>
    /// <param name="s3Client">Клиент приватного S3-совместимого хранилища.</param>
    /// <param name="options">Настройки контейнера и прикладного шифрования.</param>
    /// <param name="logger">Журнал технических ошибок без текста документа.</param>
    public S3EncryptedDocumentStorageService(
        IAmazonS3 s3Client,
        IOptions<DocumentStorageOptions> options,
        ILogger<S3EncryptedDocumentStorageService> logger)
    {
        ArgumentNullException.ThrowIfNull(s3Client);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _s3Client = s3Client;
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.BucketName))
        {
            throw new InvalidOperationException("Имя приватного контейнера документов обязательно.");
        }

        try
        {
            _encryptionKey = Convert.FromBase64String(_options.EncryptionKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException("Ключ шифрования документов должен иметь формат Base64.", exception);
        }

        if (_encryptionKey.Length != 32)
        {
            throw new InvalidOperationException("Для шифрования документов требуется AES-256-ключ длиной ровно 32 байта.");
        }
    }

    /// <summary>
    /// Шифрует UTF-8-текст уникальным nonce, сохраняет бинарный конверт в S3 и возвращает только непрозрачный ключ объекта.
    /// </summary>
    /// <param name="content">Содержимое версии документа.</param>
    /// <param name="cancellationToken">Токен отмены шифрования и загрузки.</param>
    /// <returns>Ключ объекта либо безопасная ошибка хранилища.</returns>
    public async Task<ServiceResult<string>> StoreTextAsync(
        string content,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        var storageKey = $"documents/{Guid.NewGuid():N}.bin";
        var plaintext = Encoding.UTF8.GetBytes(content);
        var envelope = Encrypt(plaintext);

        try
        {
            await using var stream = new MemoryStream(envelope, writable: false);
            var request = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = storageKey,
                InputStream = stream,
                ContentType = "application/octet-stream",
                AutoCloseStream = false
            };
            request.Metadata["encryption"] = "AES-256-GCM";
            await _s3Client.PutObjectAsync(request, cancellationToken);
            return ServiceResult<string>.Success(storageKey);
        }
        catch (AmazonS3Exception exception)
        {
            _logger.LogError(exception, "S3 отклонил сохранение зашифрованного документа.");
            return ServiceResult<string>.Failure(["Не удалось сохранить содержимое документа."]);
        }
    }

    /// <summary>
    /// Загружает бинарный конверт, проверяет допустимый внутренний ключ, аутентифицирует ciphertext и возвращает UTF-8-текст.
    /// </summary>
    /// <param name="storageKey">Внутренний ключ объекта из доменной версии.</param>
    /// <param name="cancellationToken">Токен отмены загрузки.</param>
    /// <returns>Расшифрованный текст либо безопасная ошибка целостности/хранилища.</returns>
    public async Task<ServiceResult<string>> GetTextAsync(
        string storageKey,
        CancellationToken cancellationToken)
    {
        if (!IsValidStorageKey(storageKey))
        {
            return ServiceResult<string>.Failure(["Ключ содержимого документа недопустим."]);
        }

        try
        {
            using var response = await _s3Client.GetObjectAsync(
                _options.BucketName,
                storageKey,
                cancellationToken);
            await using var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
            var plaintext = Decrypt(buffer.ToArray());
            return ServiceResult<string>.Success(Encoding.UTF8.GetString(plaintext));
        }
        catch (AmazonS3Exception exception)
        {
            _logger.LogWarning(exception, "S3 не вернул зашифрованный документ с ключом {StorageKey}.", storageKey);
            return ServiceResult<string>.Failure(["Содержимое документа не найдено или недоступно."]);
        }
        catch (CryptographicException exception)
        {
            _logger.LogError(exception, "Проверка целостности зашифрованного документа не пройдена для {StorageKey}.", storageKey);
            return ServiceResult<string>.Failure(["Целостность содержимого документа нарушена."]);
        }
    }

    /// <summary>
    /// Необратимо удаляет объект по проверенному внутреннему ключу; повторный вызов сохраняет идемпотентный успех S3.
    /// </summary>
    /// <param name="storageKey">Ключ удаляемого содержимого.</param>
    /// <param name="cancellationToken">Токен отмены удаления.</param>
    /// <returns>Признак успешного удаления либо безопасная ошибка хранилища.</returns>
    public async Task<ServiceResult<bool>> DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken)
    {
        if (!IsValidStorageKey(storageKey))
        {
            return ServiceResult<bool>.Failure(["Ключ содержимого документа недопустим."]);
        }

        try
        {
            await _s3Client.DeleteObjectAsync(_options.BucketName, storageKey, cancellationToken);
            return ServiceResult<bool>.Success(true);
        }
        catch (AmazonS3Exception exception)
        {
            _logger.LogError(exception, "S3 отклонил удаление документа с ключом {StorageKey}.", storageKey);
            return ServiceResult<bool>.Failure(["Не удалось удалить содержимое документа."]);
        }
    }

    /// <summary>Создаёт версионированный AES-GCM-конверт из открытого текста.</summary>
    /// <param name="plaintext">UTF-8-байты содержимого.</param>
    /// <returns>Версия, nonce, tag и ciphertext в одном массиве.</returns>
    private byte[] Encrypt(byte[] plaintext)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        Span<byte> associatedData = stackalloc byte[1];
        associatedData[0] = EnvelopeVersion;
        using var aes = new AesGcm(_encryptionKey, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

        var envelope = new byte[1 + NonceSize + TagSize + ciphertext.Length];
        envelope[0] = EnvelopeVersion;
        nonce.CopyTo(envelope, 1);
        tag.CopyTo(envelope, 1 + NonceSize);
        ciphertext.CopyTo(envelope, 1 + NonceSize + TagSize);
        return envelope;
    }

    /// <summary>Проверяет версию и аутентификационный тег бинарного конверта перед расшифрованием.</summary>
    /// <param name="envelope">Загруженный бинарный конверт.</param>
    /// <returns>Расшифрованные UTF-8-байты.</returns>
    private byte[] Decrypt(byte[] envelope)
    {
        if (envelope.Length < 1 + NonceSize + TagSize || envelope[0] != EnvelopeVersion)
        {
            throw new CryptographicException("Формат зашифрованного документа недопустим.");
        }

        var nonce = envelope.AsSpan(1, NonceSize);
        var tag = envelope.AsSpan(1 + NonceSize, TagSize);
        var ciphertext = envelope.AsSpan(1 + NonceSize + TagSize);
        var plaintext = new byte[ciphertext.Length];
        Span<byte> associatedData = stackalloc byte[1];
        associatedData[0] = EnvelopeVersion;
        using var aes = new AesGcm(_encryptionKey, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
        return plaintext;
    }

    /// <summary>Ограничивает операции только непрозрачными ключами собственного каталога документов.</summary>
    /// <param name="storageKey">Проверяемый ключ.</param>
    /// <returns>Признак корректного формата без обхода пути.</returns>
    private static bool IsValidStorageKey(string storageKey)
    {
        return !string.IsNullOrWhiteSpace(storageKey)
            && storageKey.StartsWith("documents/", StringComparison.Ordinal)
            && storageKey.EndsWith(".bin", StringComparison.Ordinal)
            && !storageKey.Contains("..", StringComparison.Ordinal)
            && storageKey.Length <= 500;
    }
}
