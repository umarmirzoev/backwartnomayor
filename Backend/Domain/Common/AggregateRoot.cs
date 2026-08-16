namespace Domain.Common;

/// <summary>
/// Представляет корень агрегата и обозначает границу согласованности доменной модели.
/// Изменения объектов внутри агрегата должны выполняться через методы его корня.
/// </summary>
public abstract class AggregateRoot : BaseEntity
{
    /// <summary>
    /// Инициализирует корень агрегата для материализации ORM.
    /// </summary>
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// Инициализирует новый корень агрегата заданным идентификатором.
    /// </summary>
    /// <param name="id">Непустой идентификатор агрегата.</param>
    protected AggregateRoot(Guid id)
        : base(id)
    {
    }
}
