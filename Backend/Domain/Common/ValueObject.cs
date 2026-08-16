namespace Domain.Common;

/// <summary>
/// Представляет базовый тип объекта-значения, идентичность которого определяется
/// совокупностью его компонентов, а не отдельным идентификатором.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Возвращает компоненты, участвующие в структурном сравнении объекта-значения.
    /// Порядок компонентов обязан быть стабильным для корректного вычисления равенства.
    /// </summary>
    /// <returns>Последовательность компонентов структурного равенства.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Сравнивает текущий объект-значение с другим объектом по типу и компонентам.
    /// </summary>
    /// <param name="obj">Объект для сравнения.</param>
    /// <returns><see langword="true"/>, если объекты структурно равны.</returns>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj is ValueObject other
            && GetType() == other.GetType()
            && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Вычисляет хеш-код из всех компонентов структурного равенства.
    /// </summary>
    /// <returns>Стабильный хеш-код текущего значения.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }
}
