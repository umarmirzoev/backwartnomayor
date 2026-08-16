using Domain.Exceptions;

namespace Domain.Common;

/// <summary>
/// Представляет базовый тип доменной сущности с устойчивой идентичностью.
/// Идентификатор создаётся в домене, что делает жизненный цикл сущности независимым
/// от механизма хранения и позволяет проверять объект без подключения базы данных.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Инициализирует сущность для последующей материализации ORM.
    /// Конструктор не создаёт идентификатор, поскольку сохранённое значение будет
    /// восстановлено инфраструктурным слоем.
    /// </summary>
    protected BaseEntity()
    {
    }

    /// <summary>
    /// Инициализирует новую доменную сущность заданным идентификатором.
    /// </summary>
    /// <param name="id">Непустой идентификатор сущности.</param>
    /// <exception cref="DomainValidationException">
    /// Выбрасывается, если передан пустой идентификатор.
    /// </exception>
    protected BaseEntity(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Идентификатор сущности не может быть пустым.");
        }

        Id = id;
    }

    /// <summary>
    /// Получает уникальный идентификатор сущности в пределах домена.
    /// </summary>
    public Guid Id { get; private set; }
}
