namespace Domain.Enums;

/// <summary>
/// Определяет способ электронной подписи документа в целевой версии продукта.
/// </summary>
public enum SignatureMethod
{
    /// <summary>Простая подпись с явным соглашением участников системы.</summary>
    Simple,

    /// <summary>Защищённая подпись аккредитованного удостоверяющего центра.</summary>
    AccreditedCA
}
