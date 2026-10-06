
using System;
using Microsoft.Extensions.Configuration;

namespace OpenBreweryApi.Security
{
    public class EnvApiKeyProvider : IApiKeyProvider
    {
        private readonly IConfiguration _configuration;

        public EnvApiKeyProvider(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string? GetApiKey()
        {
            // 1) Environment variable (recommended)
            var env = Environment.GetEnvironmentVariable("OPENBREWERY_API_KEY");
            if (!string.IsNullOrWhiteSpace(env))
                return env.Trim();

            // 2) Fall back to configuration (appsettings.json or User Secrets in Development)
            var configKey = _configuration["ApiKey"];
            return string.IsNullOrWhiteSpace(configKey) ? null : configKey.Trim();
        }
    }
}