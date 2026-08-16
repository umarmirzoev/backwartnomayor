namespace Domain.Enums;

/// <summary>
/// Определяет рабочее состояние дела клиента.
/// </summary>
public enum CaseStatus
{
    /// <summary>Дело находится в активной работе.</summary>
    Open,

    /// <summary>Дело закрыто юристом.</summary>
    Closed
}
