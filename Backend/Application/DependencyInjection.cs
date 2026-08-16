using Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

/// <summary>
/// Предоставляет единый модуль регистрации прикладного слоя.
/// Регистрация по сборке гарантирует обнаружение всех CQRS-обработчиков, уведомлений,
/// валидаторов и профилей AutoMapper без зависимости WebAPI от конкретных типов сценариев.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует MediatR, FluentValidation, AutoMapper и упорядоченный конвейер прикладных проверок.
    /// Валидация выполняется до обращения к обработчику, доменные нарушения преобразуются в Response,
    /// а проверка ИИ-квоты применяется только к тарифицируемым операциям.
    /// </summary>
    /// <param name="services">Коллекция сервисов Composition Root.</param>
    /// <returns>Та же коллекция для последовательной настройки приложения.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(DomainExceptionBehavior<,>));
            configuration.AddOpenBehavior(typeof(AiQuotaCheckBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddAutoMapper(configuration => { }, assembly);

        return services;
    }
}
