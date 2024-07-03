namespace Microsoft.Extensions.Configuration
{
    public static class ConfigurationExtensions
    {
        private const string WebJobsConfigurationSectionName = "AzureTableStorage";
        public static IConfigurationSection? GetWebJobsConnectionStringSection(this IConfiguration configuration, string connectionStringName)
        {
             // first try prefixing
            string prefixedConnectionStringName = GetPrefixedConnectionStringName(connectionStringName);
            IConfigurationSection? section = GetConnectionStringOrSetting(configuration, prefixedConnectionStringName);

            if (!section.Exists())
            {
                // next try a direct unprefixed lookup
                section = GetConnectionStringOrSetting(configuration, connectionStringName);
            }
            return section;
        }
         public static string GetPrefixedConnectionStringName(string connectionStringName)
        {
            return WebJobsConfigurationSectionName + connectionStringName;
        }

        /// <summary>
        /// Looks for a connection string by first checking the ConfigurationStrings section, and then the root.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="connectionName">The connection string key.</param>
        /// <returns></returns>
        public static IConfigurationSection? GetConnectionStringOrSetting(this IConfiguration configuration, string connectionName)
        {
            var connectionStringSection = configuration?.GetSection("ConnectionStrings").GetSection(connectionName);

            if (connectionStringSection?.Exists() == true)
            {
                return connectionStringSection;
            }
            return configuration?.GetSection(connectionName);
        }
    }
}