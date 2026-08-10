namespace KeepApi.Infrastructure.Authentication.External
{
    /// <summary>appsettings.json'daki "ExternalProviders" bölümünün karşılığı.</summary>
    public sealed class ExternalProvidersSettings
    {
        public const string SectionName = "ExternalProviders";

        public ExternalProviderCredentials Google { get; init; } = new();

        public ExternalProviderCredentials Microsoft { get; init; } = new();

        public ExternalProviderCredentials GitHub { get; init; } = new();
    }

    public sealed class ExternalProviderCredentials
    {
        public string ClientId { get; init; } = string.Empty;

        public string ClientSecret { get; init; } = string.Empty;
    }
}