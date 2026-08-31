using System;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace OSDC.Drilling.Rig.Service.Managers
{
    /// <summary>
    /// A manager for the sql database connection, registered as a singleton through dependency injection (see Program.cs)
    /// Prior to creating a database, existing database structure is checked for consistency with the structure defined in tableStructureDict_
    /// If inconsistent (table count, table names, fields count, fields names), a timestamped backup of the existing database is generated first
    /// </summary>
    /// <remarks>
    /// SQLite database connection strategy:
    /// - single connection for every access (chosen strategy in the general case)
    ///     each access to the database is performed through isolated connections stored in a List of connections
    ///     > isolation, reliability, fail-safe, thread-safe, but overhead due to opening connections
    /// - shared connection between access
    ///     one connection is opened for the lifetime of the application and used to access database through various web requests and commands 
    ///     > no overhead, but issues with concurrency, single-point of failure, state management
    /// - scoped connection (registering service with AddScoped rather than AddSingleton)
    ///     one connection is opened per web request
    ///     > same problems as with shared connection, but limited to the scope of one webrequest rather than to the whole lifetime of the application
    /// </remarks>
    public class SqlConnectionManager
    {
        private readonly ILogger<SqlConnectionManager> _logger;
        private readonly string _connectionString;
        public static readonly string HOME_DIRECTORY = ".." + Path.DirectorySeparatorChar + "home" + Path.DirectorySeparatorChar;
        public static readonly string DATABASE_FILENAME = "Rig.db";
        public static readonly string DATE_TIME_FORMAT = "O";

        // dictionary describing tables format
        // Light weight data fields are enumerated explicitly in the data table implementing the light weight data concept
        // (thus duplicating info in the database) for 2 reasons
        // 1) to avoid loading the complete Rig (heavy weight data) each time we only need contextual info on the data (light weight data)
        // 2) to keep control of the logic of inserting and selecting a light data in the database
        //    localized at the controller/manager level (storing RigLight as a whole could induce database corruption issues)
        // If the light weight data concept is not implemented, the same contextual info can be retrieved directly from the Rig
        private readonly static Dictionary<string, string[]> _tableStructureDict = new Dictionary<string, string[]>()
            {
                { "RigTable", new string[] {
                    "MetaInfo text",
                    // beginning of list of fields used only when light weight concept is implemented
                    "Name text",
                    "Description text",
                    "CreationDate text",
                    "LastModificationDate text",
                    "IsFixedPlatform bool",
                    "ClusterID text",
                    // end of list of fields used only when light weight concept is implemented
                    "data text" }
                },
                { "RigFeatureCategoryTable", new string[] {
                    "MetaInfo text",
                    "Code text",
                    "Name text",
                    "IsExclusive bool",
                    "HasValidityPeriod bool",
                    "IsBuiltIn bool",
                    "CreationDate text",
                    "LastModificationDate text",
                    "data text" }
                },
                { "RigPhotoTable", new string[] {
                    "MetaInfo text", "RigID text", "DisplayOrder integer", "IsPrimary bool",
                    "ContentType text", "FileName text", "ByteLength integer", "Sha256 text",
                    "CreationDate text", "LastModificationDate text", "data text", "Content blob" }
                }
            };

        public SqlConnectionManager(string connectionString, ILogger<SqlConnectionManager> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
            _logger.LogInformation("SqliteConnectionManager created");
            if (Initialize())
            {
                ManageDataBase();
            }
            else
            {
                _logger.LogInformation("SqliteConnectionManager created");
            }
        }

        public SqliteConnection? GetConnection()
        {
            // a new SQL connection is opened for every transaction, thus ensuring thread-safety and removing unnecessary locks
            var connection = new SqliteConnection(_connectionString);
            if (connection != null)
            {
                connection.Open();
            }
            else
            {
                _logger.LogError("Problem while opening SQLite connection");
            }
            return connection;
        }

        private bool Initialize()
        {
            if (!Directory.Exists(HOME_DIRECTORY))
            {
                _logger.LogInformation("Creating home directory");
                try
                {
                    Directory.CreateDirectory(HOME_DIRECTORY);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Impossible to create home directory for local storage");
                    return false;
                }
            }
            if (Directory.Exists(HOME_DIRECTORY))
            {
                try
                {
                    string databaseFileName = HOME_DIRECTORY + Path.DirectorySeparatorChar + DATABASE_FILENAME;
                    if (File.Exists(databaseFileName))
                    {
                        _logger.LogInformation("Opening database {_databaseFileName}", DATABASE_FILENAME);
                    }
                    else
                    {
                        _logger.LogInformation("Creating database {_databaseFileName}", DATABASE_FILENAME);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Impossible to create {_databaseFileName}", DATABASE_FILENAME);
                    return false;
                }
            }
            else
            {
                _logger.LogError("Home directory for local storage should have been created, check for access");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Ensures that every managed table exists with the expected shape. Missing tables are added
        /// without touching existing data. An incompatible managed table is backed up and rebuilt in
        /// isolation; unrelated tables are preserved.
        /// </summary>
        private void ManageDataBase()
        {
            using var connection = GetConnection();
            if (connection == null)
            {
                _logger.LogError("Problem opening a new connection while managing database");
                return;
            }

            List<string> tableNames = [];
            using (var command = new SqliteCommand("SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';", connection))
            {
                using var reader = command.ExecuteReader();
                while (reader.Read()) tableNames.Add(reader.GetString(0));
            }

            bool backupCreated = false;
            foreach (var tableStructure in _tableStructureDict)
            {
                string tableName = tableStructure.Key;
                if (!tableNames.Contains(tableName, StringComparer.Ordinal))
                {
                    _logger.LogInformation("Adding missing database table {tableName}", tableName);
                    if (!CreateTable(tableStructure) || !IndexTable(tableName))
                        throw new InvalidOperationException($"Unable to create required database table {tableName}.");
                    continue;
                }

                if (CheckDatabaseStructure(tableStructure)) continue;

                if (!backupCreated)
                {
                    string source = Path.Combine(HOME_DIRECTORY, DATABASE_FILENAME);
                    string stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ");
                    string backup = Path.Combine(HOME_DIRECTORY, Path.GetFileNameWithoutExtension(DATABASE_FILENAME) + $".schema-{stamp}.db");
                    File.Copy(source, backup, overwrite: false);
                    backupCreated = true;
                    _logger.LogWarning("Created database backup {backup} before rebuilding an incompatible managed table", backup);
                }

                _logger.LogWarning("Rebuilding incompatible managed table {tableName}; other tables are preserved", tableName);
                if (!DropTable(tableName) || !CreateTable(tableStructure) || !IndexTable(tableName))
                    throw new InvalidOperationException($"Unable to rebuild incompatible database table {tableName}.");
            }
        }

        /// <summary>
        /// Check that expected fields (in tableStructure.Value) exactly match those of the stored database
        /// </summary>
        /// <param name="tableStructure"></param>
        /// <returns>true if the expected fields exactly match fields of the stored database</returns>
        private bool CheckDatabaseStructure(KeyValuePair<string, string[]> tableStructure)
        {
            using var connection = GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                string key = tableStructure.Key;
                StringBuilder sb = new StringBuilder();
                sb.Append($"SELECT * FROM {key}");
                command.CommandText = sb.ToString();
                try
                {
                    using (var reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
                    {
                        var schema = reader.GetSchemaTable();
                        if (tableStructure.Value.Length != schema.Rows.Count)
                            return false; // unexpected number of fields in table
                        foreach (string field in tableStructure.Value)
                        {
                            bool tmpSuccess = false;
                            foreach (DataRow col in schema.Rows)
                            {
                                if (field.Split(" ").ElementAt(0) == col.Field<string>("ColumnName"))
                                {
                                    tmpSuccess = true;
                                    break;
                                }
                            }
                            if (!tmpSuccess)
                                return false; // at least one expected field is not found in stored database
                        }
                    }
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to retrieve schema from table {key}", key);
                    return false;
                }
            }
            else
            {
                _logger.LogError("Problem opening a new connection while checking database structure");
                return false;
            }
            return true;
        }

        private bool CreateTable(KeyValuePair<string, string[]> tabStruct)
        {
            using var connection = GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                string key = tabStruct.Key;
                StringBuilder sb = new StringBuilder();
                sb.Append($"CREATE TABLE {key} ()");
                foreach (string col in tabStruct.Value)
                {
                    sb.Insert(sb.Length - 1, col + ",");
                };
                sb.Remove(sb.Length - 2, 1);
                command.CommandText = sb.ToString();

                try
                {
                    int res = command.ExecuteNonQuery();
                    _logger.LogInformation("{key} has been successfully created", key);
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to create {key} which will be dropped", key);
                    return false;
                }
            }
            else
            {
                _logger.LogError("Problem opening a new connection while creating table");
                return false;
            }
            return true;
        }

        private bool IndexTable(string dbName)
        {
            using var connection = GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = $"CREATE UNIQUE INDEX {dbName}MetaInfoIdIndex ON {dbName} (json_extract(MetaInfo, '$.ID'))";
                try
                {
                    int res = command.ExecuteNonQuery();
                    _logger.LogInformation("{dbName} has been successfully indexed", dbName);
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to index {dbName} which will be dropped", dbName);
                    return false;
                }
            }
            else
            {
                _logger.LogError("Problem opening a new connection while creating table");
                return false;
            }
            return true;
        }

        private bool DropTable(string dbName)
        {
            using var connection = GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText =
                            $"DROP TABLE {dbName}";
                try
                {
                    int res = command.ExecuteNonQuery();
                    _logger.LogWarning("{dbName} has been successfully dropped", dbName);
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to drop {dbName}", dbName);
                    return false;
                }
            }
            else
            {
                _logger.LogError("Problem opening a new connection while creating table");
                return false;
            }
            return true;
        }
    }
}
