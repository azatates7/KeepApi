// KeepApi.Infrastructure/Configuration/DbSettingsConfigurationProvider.cs
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace KeepApi.Infrastructure.Configuration
{
    public class DbSettingsConfigurationSource : IConfigurationSource
    {
        private readonly string _connectionString;
        private readonly string _targetProject;
        private readonly IDataProtectionProvider _protectionProvider;

        public DbSettingsConfigurationSource(string connectionString, string targetProject, IDataProtectionProvider protectionProvider)
        {
            _connectionString = connectionString;
            _targetProject = targetProject;
            _protectionProvider = protectionProvider;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
            => new DbSettingsConfigurationProvider(_connectionString, _targetProject, _protectionProvider);
    }

    public class DbSettingsConfigurationProvider : ConfigurationProvider
    {
        private readonly string _connectionString;
        private readonly string _targetProject;
        private readonly IDataProtectionProvider _protectionProvider;

        public DbSettingsConfigurationProvider(string connectionString, string targetProject, IDataProtectionProvider protectionProvider)
        {
            _connectionString = connectionString;
            _targetProject = targetProject;
            _protectionProvider = protectionProvider;
        }

        public override void Load()
        {
            var protector = _protectionProvider.CreateProtector("AppSettings.Secrets");

            using var connection = new OracleConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT SETTING_KEY, SETTING_VALUE, IS_ENCRYPTED FROM APP_SETTINGS WHERE TARGET_PROJECT = :project";
            command.Parameters.Add(new OracleParameter("project", _targetProject));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var key = reader.GetString(0);
                var rawValue = reader.GetString(1);
                var isEncrypted = reader.GetInt32(2) == 1;

                Data[key] = isEncrypted ? protector.Unprotect(rawValue) : rawValue;
            }
        }
    }
}