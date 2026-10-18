using FluentResults;

namespace flight_service.Application.Features.ConfigurationFeatures.Errors;

public enum ConfigurationErrorKind
{
    NotFound,
    Conflict
}

public sealed class ConfigurationError(string code, string message, ConfigurationErrorKind kind)
    : Error(message)
{
    public string Code { get; } = code;
    public ConfigurationErrorKind Kind { get; } = kind;

    public static ConfigurationError Missing(string resource)
    {
        return new ConfigurationError($"{resource}_not_found", $"{resource.Replace('_', ' ')} was not found.",
            ConfigurationErrorKind.NotFound);
    }

    public static ConfigurationError Conflict(string code, string message)
    {
        return new ConfigurationError(code, message, ConfigurationErrorKind.Conflict);
    }
}
