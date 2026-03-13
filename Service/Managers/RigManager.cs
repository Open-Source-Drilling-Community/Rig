using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Data;

namespace NORCE.Drilling.Rig.Service.Managers
{
    /// <summary>
    /// A manager for Rig. The manager implements the singleton pattern as defined by 
    /// Gamma, Erich, et al. "Design patterns: Abstraction and reuse of object-oriented design." 
    /// European Conference on Object-Oriented Programming. Springer, Berlin, Heidelberg, 1993.
    /// </summary>
    public class RigManager
    {
        private const string RigTableName = "RigTable";
        private const string MetaInfoIdJsonPath = "$.ID";
        private static RigManager? _instance = null;
        private readonly ILogger<RigManager> _logger;
        private readonly SqlConnectionManager _connectionManager;

        private RigManager(ILogger<RigManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionManager = connectionManager;
        }

        public static RigManager GetInstance(ILogger<RigManager> logger, SqlConnectionManager connectionManager)
        {
            _instance ??= new RigManager(logger, connectionManager);
            return _instance;
        }

        public int Count
        {
            get
            {
                int count = 0;
                using var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = $"SELECT COUNT(*) FROM {RigTableName}";
                    try
                    {
                        using SqliteDataReader reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            count = (int)reader.GetInt64(0);
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to count records in the RigTable");
                    }
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
                return count;
            }
        }

        public bool Clear()
        {
            using var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                bool success = false;
                using var transaction = connection.BeginTransaction();
                try
                {
                    //empty RigTable
                    var command = connection.CreateCommand();
                    command.CommandText = $"DELETE FROM {RigTableName}";
                    command.ExecuteNonQuery();

                    transaction.Commit();
                    success = true;
                }
                catch (SqliteException ex)
                {
                    transaction.Rollback();
                    _logger.LogError(ex, "Impossible to clear the RigTable");
                }
                return success;
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
                return false;
            }
        }

        public bool Contains(Guid guid)
        {
            int count = 0;
            using var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT COUNT(*) FROM {RigTableName} WHERE json_extract(MetaInfo, '{MetaInfoIdJsonPath}') = $id";
                command.Parameters.AddWithValue("$id", guid.ToString());
                try
                {
                    using SqliteDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        count = (int)reader.GetInt64(0);
                    }
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to count rows from RigTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return count >= 1;
        }

        /// <summary>
        /// Returns the list of Guid of all Rig present in the microservice database 
        /// </summary>
        /// <returns>the list of Guid of all Rig present in the microservice database</returns>
        public List<Guid>? GetAllRigId()
        {
            List<Guid> ids = [];
            using var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT MetaInfo FROM {RigTableName}";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        MetaInfo? metaInfo = DeserializeMetaInfo(reader.GetString(0));
                        if (metaInfo != null && metaInfo.ID != Guid.Empty)
                        {
                            ids.Add(metaInfo.ID);
                        }
                    }
                    _logger.LogInformation("Returning the list of ID of existing records from RigTable");
                    return ids;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get IDs from RigTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of MetaInfo of all Rig present in the microservice database 
        /// </summary>
        /// <returns>the list of MetaInfo of all Rig present in the microservice database</returns>
        public List<MetaInfo?>? GetAllRigMetaInfo()
        {
            List<MetaInfo?> metaInfos = new();
            using var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT MetaInfo FROM {RigTableName}";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        metaInfos.Add(DeserializeMetaInfo(reader.GetString(0)));
                    }
                    _logger.LogInformation("Returning the list of MetaInfo of existing records from RigTable");
                    return metaInfos;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get IDs from RigTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the Rig identified by its Guid from the microservice database 
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>the Rig identified by its Guid from the microservice database</returns>
        public Model.Rig? GetRigById(Guid guid)
        {
            if (!guid.Equals(Guid.Empty))
            {
                using var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    Model.Rig? rig;
                    var command = connection.CreateCommand();
                    command.CommandText = $"SELECT data FROM {RigTableName} WHERE json_extract(MetaInfo, '{MetaInfoIdJsonPath}') = $id";
                    command.Parameters.AddWithValue("$id", guid.ToString());
                    try
                    {
                        using var reader = command.ExecuteReader();
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            string data = reader.GetString(0);
                            rig = JsonSerializer.Deserialize<Model.Rig>(data, JsonSettings.Options);
                            if (rig != null && rig.MetaInfo != null && !rig.MetaInfo.ID.Equals(guid))
                                throw new SqliteException("SQLite database corrupted: returned Rig is null or has been jsonified with the wrong ID.", 1);
                        }
                        else
                        {
                            _logger.LogInformation("No Rig of given ID in the database");
                            return null;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to get the Rig with the given ID from RigTable");
                        return null;
                    }
                    _logger.LogInformation("Returning the Rig of given ID from RigTable");
                    return rig;
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The given Rig ID is null or empty");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of all Rig present in the microservice database 
        /// </summary>
        /// <returns>the list of all Rig present in the microservice database</returns>
        public List<Model.Rig?>? GetAllRig()
        {
            List<Model.Rig?> vals = [];
            using var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT data FROM {RigTableName}";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        string data = reader.GetString(0);
                        Model.Rig? rig = JsonSerializer.Deserialize<Model.Rig>(data, JsonSettings.Options);
                        vals.Add(rig);
                    }
                    _logger.LogInformation("Returning the list of existing Rig from RigTable");
                    return vals;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get Rig from RigTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Returns the list of all RigLight present in the microservice database 
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>the list of RigLight present in the microservice database</returns>
        public List<Model.RigLight>? GetAllRigLight()
        {
            List<Model.RigLight>? rigLightList = [];
            using var connection = _connectionManager.GetConnection();
            if (connection != null)
            {
                var command = connection.CreateCommand();
                command.CommandText = $"SELECT MetaInfo, Name, Description, CreationDate, LastModificationDate, IsFixedPlatform, ClusterID FROM {RigTableName}";
                try
                {
                    using var reader = command.ExecuteReader();
                    while (reader.Read() && !reader.IsDBNull(0))
                    {
                        MetaInfo? metaInfo = DeserializeMetaInfo(reader.GetString(0));
                        string? name = reader.IsDBNull(1) ? null : reader.GetString(1);
                        string? descr = reader.IsDBNull(2) ? null : reader.GetString(2);
                        // make sure DateTimeOffset are properly instantiated when stored values are null (and parsed as empty string)
                        DateTimeOffset? creationDate = TryReadDateTimeOffset(reader, 3);
                        DateTimeOffset? lastModificationDate = TryReadDateTimeOffset(reader, 4);
                        bool isFixedPlatform = !reader.IsDBNull(5) && reader.GetBoolean(5);
                        Guid? clusterID = null;
                        if (!reader.IsDBNull(6) && Guid.TryParse(reader.GetString(6), out Guid id))
                        {
                            clusterID = id;
                        }
                        rigLightList.Add(new Model.RigLight(
                                metaInfo,
                                string.IsNullOrEmpty(name) ? null : name,
                                string.IsNullOrEmpty(descr) ? null : descr,
                                creationDate,
                                lastModificationDate,
                                isFixedPlatform,
                                clusterID
                                ));
                    }
                    _logger.LogInformation("Returning the list of existing RigLight from RigTable");
                    return rigLightList;
                }
                catch (SqliteException ex)
                {
                    _logger.LogError(ex, "Impossible to get light datas from RigTable");
                }
            }
            else
            {
                _logger.LogWarning("Impossible to access the SQLite database");
            }
            return null;
        }

        /// <summary>
        /// Performs calculation on the given Rig and adds it to the microservice database
        /// </summary>
        /// <param name="rig"></param>
        /// <returns>true if the given Rig has been added successfully to the microservice database</returns>
        public bool AddRig(Model.Rig? rig)
        {
            if (rig != null && rig.MetaInfo != null && rig.MetaInfo.ID != Guid.Empty)
            {
                //if successful, check if another parent data with the same ID was calculated/added during the calculation time
                Model.Rig? newRig = GetRigById(rig.MetaInfo.ID);
                if (newRig == null)
                {
                    //update RigTable
                    using var connection = _connectionManager.GetConnection();
                    if (connection != null)
                    {
                        using SqliteTransaction transaction = connection.BeginTransaction();
                        bool success = true;
                        try
                        {
                            //add the Rig to the RigTable
                            string metaInfo = JsonSerializer.Serialize(rig.MetaInfo, JsonSettings.Options);
                            string? cDate = FormatDateTimeOffset(rig.CreationDate);
                            string? lDate = FormatDateTimeOffset(rig.LastModificationDate);
                            string data = JsonSerializer.Serialize(rig, JsonSettings.Options);
                            var command = connection.CreateCommand();
                            command.Transaction = transaction;
                            command.CommandText = $"INSERT INTO {RigTableName} (" +
                                "MetaInfo, " +
                                "Name, " +
                                "Description, " +
                                "CreationDate, " +
                                "LastModificationDate, " +
                                "IsFixedPlatform, " +
                                "ClusterID, " +
                                "data" +
                                ") VALUES (" +
                                "$metaInfo, " +
                                "$name, " +
                                "$description, " +
                                "$creationDate, " +
                                "$lastModificationDate, " +
                                "$isFixedPlatform, " +
                                "$clusterId, " +
                                "$data" +
                                ")";
                            AddRigParameters(command, rig, metaInfo, cDate, lDate, data);
                            int count = command.ExecuteNonQuery();
                            if (count != 1)
                            {
                                _logger.LogWarning("Impossible to insert the given Rig into the RigTable");
                                success = false;
                            }
                        }
                        catch (SqliteException ex)
                        {
                            _logger.LogError(ex, "Impossible to add the given Rig into RigTable");
                            success = false;
                        }
                        //finalizing SQL transaction
                        if (success)
                        {
                            transaction.Commit();
                            _logger.LogInformation("Added the given Rig of given ID into the RigTable successfully");
                        }
                        else
                        {
                            transaction.Rollback();
                        }
                        return success;
                    }
                    else
                    {
                        _logger.LogWarning("Impossible to access the SQLite database");
                    }
                }
                else
                {
                    _logger.LogWarning("Impossible to post Rig. ID already found in database.");
                    return false;
                }

            }
            else
            {
                _logger.LogWarning("The Rig ID or the ID of its input are null or empty");
            }
            return false;
        }

        /// <summary>
        /// Performs calculation on the given Rig and updates it in the microservice database
        /// </summary>
        /// <param name="rig"></param>
        /// <returns>true if the given Rig has been updated successfully</returns>
        public bool UpdateRigById(Guid guid, Model.Rig? rig)
        {
            bool success = true;
            if (guid != Guid.Empty && rig != null && rig.MetaInfo != null && rig.MetaInfo.ID == guid)
            {
                //update RigTable
                using var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    using SqliteTransaction transaction = connection.BeginTransaction();
                    //update fields in RigTable
                    try
                    {
                        string metaInfo = JsonSerializer.Serialize(rig.MetaInfo, JsonSettings.Options);
                        string? cDate = FormatDateTimeOffset(rig.CreationDate);
                        rig.LastModificationDate = DateTimeOffset.UtcNow;
                        string? lDate = FormatDateTimeOffset(rig.LastModificationDate);
                        string data = JsonSerializer.Serialize(rig, JsonSettings.Options);
                        var command = connection.CreateCommand();
                        command.Transaction = transaction;
                        command.CommandText = $"UPDATE {RigTableName} SET " +
                            "MetaInfo = $metaInfo, " +
                            "Name = $name, " +
                            "Description = $description, " +
                            "CreationDate = $creationDate, " +
                            "LastModificationDate = $lastModificationDate, " +
                            "IsFixedPlatform = $isFixedPlatform, " +
                            "ClusterID = $clusterId, " +
                            "data = $data " +
                            $"WHERE json_extract(MetaInfo, '{MetaInfoIdJsonPath}') = $id";
                        AddRigParameters(command, rig, metaInfo, cDate, lDate, data);
                        command.Parameters.AddWithValue("$id", guid.ToString());
                        int count = command.ExecuteNonQuery();
                        if (count != 1)
                        {
                            _logger.LogWarning("Impossible to update the Rig");
                            success = false;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to update the Rig");
                        success = false;
                    }

                    // Finalizing
                    if (success)
                    {
                        transaction.Commit();
                        _logger.LogInformation("Updated the given Rig successfully");
                        return true;
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The Rig ID or the ID of some of its attributes are null or empty");
            }
            return false;
        }

        /// <summary>
        /// Deletes the Rig of given ID from the microservice database
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>true if the Rig was deleted from the microservice database</returns>
        public bool DeleteRigById(Guid guid)
        {
            if (!guid.Equals(Guid.Empty))
            {
                using var connection = _connectionManager.GetConnection();
                if (connection != null)
                {
                    using var transaction = connection.BeginTransaction();
                    bool success = true;
                    //delete Rig from RigTable
                    try
                    {
                        var command = connection.CreateCommand();
                        command.Transaction = transaction;
                        command.CommandText = $"DELETE FROM {RigTableName} WHERE json_extract(MetaInfo, '{MetaInfoIdJsonPath}') = $id";
                        command.Parameters.AddWithValue("$id", guid.ToString());
                        int count = command.ExecuteNonQuery();
                        if (count < 0)
                        {
                            _logger.LogWarning("Impossible to delete the Rig of given ID from the RigTable");
                            success = false;
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogError(ex, "Impossible to delete the Rig of given ID from RigTable");
                        success = false;
                    }
                    if (success)
                    {
                        transaction.Commit();
                        _logger.LogInformation("Removed the Rig of given ID from the RigTable successfully");
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                    return success;
                }
                else
                {
                    _logger.LogWarning("Impossible to access the SQLite database");
                }
            }
            else
            {
                _logger.LogWarning("The Rig ID is null or empty");
            }
            return false;
        }

        private static MetaInfo? DeserializeMetaInfo(string json) =>
            JsonSerializer.Deserialize<MetaInfo>(json, JsonSettings.Options);

        private static DateTimeOffset? TryReadDateTimeOffset(SqliteDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            string value = reader.GetString(ordinal);
            return DateTimeOffset.TryParse(value, out DateTimeOffset parsed) ? parsed : null;
        }

        private static string? FormatDateTimeOffset(DateTimeOffset? value) =>
            value?.ToString(SqlConnectionManager.DATE_TIME_FORMAT);

        private static void AddRigParameters(SqliteCommand command, Model.Rig rig, string metaInfo, string? creationDate, string? lastModificationDate, string data)
        {
            command.Parameters.AddWithValue("$metaInfo", metaInfo);
            command.Parameters.AddWithValue("$name", (object?)rig.Name ?? DBNull.Value);
            command.Parameters.AddWithValue("$description", (object?)rig.Description ?? DBNull.Value);
            command.Parameters.AddWithValue("$creationDate", (object?)creationDate ?? DBNull.Value);
            command.Parameters.AddWithValue("$lastModificationDate", (object?)lastModificationDate ?? DBNull.Value);
            command.Parameters.AddWithValue("$isFixedPlatform", rig.IsFixedPlatform ? 1 : 0);
            command.Parameters.AddWithValue("$clusterId", rig.ClusterID?.ToString() ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$data", data);
        }
    }
}
