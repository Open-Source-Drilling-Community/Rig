using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Rig.Model;

namespace OSDC.Drilling.Rig.Service.Managers;

public sealed class RigPhotoManager
{
    public const long MaximumBytes = 10 * 1024 * 1024;
    private const string Table = "RigPhotoTable";
    private static readonly HashSet<string> AllowedTypes = ["image/jpeg", "image/png", "image/webp"];
    private readonly SqlConnectionManager _connections;

    public RigPhotoManager(SqlConnectionManager connections) => _connections = connections;

    public List<RigPhotoMetadata> GetAll(Guid rigId)
    {
        using SqliteConnection connection = _connections.GetConnection()!;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT data FROM {Table} WHERE RigID=$rig ORDER BY IsPrimary DESC, DisplayOrder, CreationDate";
        command.Parameters.AddWithValue("$rig", rigId.ToString());
        using SqliteDataReader reader = command.ExecuteReader();
        List<RigPhotoMetadata> values = [];
        while (reader.Read()) values.Add(JsonSerializer.Deserialize<RigPhotoMetadata>(reader.GetString(0), JsonSettings.Options)!);
        return values;
    }

    public (RigPhotoMetadata Metadata, byte[] Content)? Get(Guid rigId, Guid photoId)
    {
        using SqliteConnection connection = _connections.GetConnection()!;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT data,Content FROM {Table} WHERE RigID=$rig AND json_extract(MetaInfo,'$.ID')=$id";
        command.Parameters.AddWithValue("$rig", rigId.ToString()); command.Parameters.AddWithValue("$id", photoId.ToString());
        using SqliteDataReader reader = command.ExecuteReader();
        return reader.Read() ? (JsonSerializer.Deserialize<RigPhotoMetadata>(reader.GetString(0), JsonSettings.Options)!, (byte[])reader[1]) : null;
    }

    public RigPhotoMetadata? Create(Guid rigId, string fileName, string contentType, byte[] content, RigPhotoMetadata input, out string? error)
    {
        error = Validate(contentType, content);
        if (error != null) return null;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        RigPhotoMetadata value = new()
        {
            MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, RigID = rigId,
            FileName = Path.GetFileName(fileName), Title = input.Title, Caption = input.Caption,
            AlternativeText = input.AlternativeText, ContentType = contentType, ByteLength = content.LongLength,
            Sha256 = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant(), DisplayOrder = input.DisplayOrder,
            IsPrimary = input.IsPrimary, Source = input.Source, Attribution = input.Attribution, License = input.License,
            CreationDate = now, LastModificationDate = now
        };
        using SqliteConnection connection = _connections.GetConnection()!; using SqliteTransaction transaction = connection.BeginTransaction();
        if (value.IsPrimary) ClearPrimary(connection, transaction, rigId);
        Write(connection, transaction, value, content, false); transaction.Commit(); return value;
    }

    public RigPhotoMetadata? Update(Guid rigId, Guid photoId, DateTimeOffset expected, RigPhotoMetadata input, out string? error)
    {
        error = null; var stored = Get(rigId, photoId); if (stored is null) { error = "not_found"; return null; }
        if (stored.Value.Metadata.LastModificationDate != expected) { error = "concurrency_conflict"; return null; }
        RigPhotoMetadata value = stored.Value.Metadata;
        value.Title=input.Title; value.Caption=input.Caption; value.AlternativeText=input.AlternativeText; value.DisplayOrder=input.DisplayOrder;
        value.IsPrimary=input.IsPrimary; value.Source=input.Source; value.Attribution=input.Attribution; value.License=input.License; value.LastModificationDate=DateTimeOffset.UtcNow;
        using SqliteConnection connection=_connections.GetConnection()!; using SqliteTransaction transaction=connection.BeginTransaction();
        if(value.IsPrimary) ClearPrimary(connection,transaction,rigId); Write(connection,transaction,value,stored.Value.Content,true); transaction.Commit(); return value;
    }

    public bool Delete(Guid rigId, Guid photoId)
    {
        using SqliteConnection connection=_connections.GetConnection()!; using SqliteCommand command=connection.CreateCommand();
        command.CommandText=$"DELETE FROM {Table} WHERE RigID=$rig AND json_extract(MetaInfo,'$.ID')=$id";
        command.Parameters.AddWithValue("$rig",rigId.ToString()); command.Parameters.AddWithValue("$id",photoId.ToString()); return command.ExecuteNonQuery()==1;
    }

    public void DeleteAll(Guid rigId) { using SqliteConnection c=_connections.GetConnection()!; using SqliteCommand q=c.CreateCommand(); q.CommandText=$"DELETE FROM {Table} WHERE RigID=$rig"; q.Parameters.AddWithValue("$rig",rigId.ToString()); q.ExecuteNonQuery(); }

    private static string? Validate(string type, byte[] content)
    {
        if (!AllowedTypes.Contains(type)) return "unsupported_content_type";
        if (content.Length == 0 || content.LongLength > MaximumBytes) return "invalid_file_size";
        bool signature = type switch { "image/jpeg" => content.Length>2&&content[0]==0xff&&content[1]==0xd8&&content[2]==0xff, "image/png" => content.Length>7&&content[..8].SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}), "image/webp" => content.Length>11&&System.Text.Encoding.ASCII.GetString(content,0,4)=="RIFF"&&System.Text.Encoding.ASCII.GetString(content,8,4)=="WEBP", _=>false };
        return signature ? null : "invalid_image_signature";
    }

    private static void ClearPrimary(SqliteConnection c, SqliteTransaction t, Guid rigId) { using SqliteCommand q=c.CreateCommand();q.Transaction=t;q.CommandText=$"UPDATE {Table} SET IsPrimary=0, data=json_set(data,'$.IsPrimary',json('false')) WHERE RigID=$rig";q.Parameters.AddWithValue("$rig",rigId.ToString());q.ExecuteNonQuery(); }
    private static void Write(SqliteConnection c, SqliteTransaction t, RigPhotoMetadata v, byte[] bytes, bool update)
    {
        string sql=update?$"UPDATE {Table} SET DisplayOrder=$ord,IsPrimary=$primary,data=$data,LastModificationDate=$modified WHERE json_extract(MetaInfo,'$.ID')=$id":$"INSERT INTO {Table}(MetaInfo,RigID,DisplayOrder,IsPrimary,ContentType,FileName,ByteLength,Sha256,CreationDate,LastModificationDate,data,Content) VALUES($meta,$rig,$ord,$primary,$type,$file,$length,$sha,$created,$modified,$data,$content)";
        using SqliteCommand q=c.CreateCommand();q.Transaction=t;q.CommandText=sql;
        q.Parameters.AddWithValue("$id",v.MetaInfo!.ID.ToString());q.Parameters.AddWithValue("$meta",JsonSerializer.Serialize(v.MetaInfo,JsonSettings.Options));q.Parameters.AddWithValue("$rig",v.RigID.ToString());q.Parameters.AddWithValue("$ord",v.DisplayOrder);q.Parameters.AddWithValue("$primary",v.IsPrimary);q.Parameters.AddWithValue("$type",v.ContentType!);q.Parameters.AddWithValue("$file",v.FileName!);q.Parameters.AddWithValue("$length",v.ByteLength);q.Parameters.AddWithValue("$sha",v.Sha256!);q.Parameters.AddWithValue("$created",v.CreationDate!.Value.ToString("O"));q.Parameters.AddWithValue("$modified",v.LastModificationDate!.Value.ToString("O"));q.Parameters.AddWithValue("$data",JsonSerializer.Serialize(v,JsonSettings.Options));q.Parameters.AddWithValue("$content",bytes);q.ExecuteNonQuery();
    }
}
