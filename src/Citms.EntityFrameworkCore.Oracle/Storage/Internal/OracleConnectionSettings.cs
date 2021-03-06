// Copyright (c) Pomelo Foundation. All rights reserved.
// Licensed under the MIT. See LICENSE in the project root for license information.

using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using Oracle.ManagedDataAccess.Client;

namespace EFCore.Oracle.Storage.Internal
{
    public class OracleConnectionSettings
    {
        private static readonly ConcurrentDictionary<string, OracleConnectionSettings> Settings
            = new ConcurrentDictionary<string, OracleConnectionSettings>();

        private static OracleConnectionStringBuilder _settingsCsb(OracleConnectionStringBuilder csb)
        {
            return csb;
        }

        public static OracleConnectionSettings GetSettings(string connectionString)
        {
            var csb = new OracleConnectionStringBuilder(connectionString);
            var settingsCsb = _settingsCsb(csb);
            return Settings.GetOrAdd(settingsCsb.ConnectionString, key =>
            {
                csb.Pooling = false;
                string serverVersion;
                using (var schemalessConnection = new OracleConnection(csb.ConnectionString))
                {
                    schemalessConnection.Open();
                    serverVersion = schemalessConnection.ServerVersion;
                }
                var version = new ServerVersion(serverVersion);
                return new OracleConnectionSettings(settingsCsb, version);
            });
        }

        public static OracleConnectionSettings GetSettings(DbConnection connection)
        {
            var csb = new OracleConnectionStringBuilder(connection.ConnectionString);
            var settingsCsb = _settingsCsb(csb);
            return Settings.GetOrAdd(settingsCsb.ConnectionString, key =>
            {
                var opened = false;
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                    opened = true;
                }
                try
                {
                    var version = new ServerVersion(connection.ServerVersion);
                    var connectionSettings = new OracleConnectionSettings(settingsCsb, version);
                    return connectionSettings;
                }
                finally
                {
                    if (opened)
                        connection.Close();
                }
            });
        }

        internal OracleConnectionSettings(OracleConnectionStringBuilder settingsCsb, ServerVersion serverVersion)
        {
            ServerVersion = serverVersion;
        }

        public readonly ServerVersion ServerVersion;
    }
}
