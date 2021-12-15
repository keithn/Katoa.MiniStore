using Microsoft.Data.Sqlite;

namespace Katoa.MiniStore;

/**
 * Embarrassingly simple keyvalue store using Sqlite.
 *
 * This is not meant to be optimized as a high speed store, it's goal is just to provide a simple store.  Even though
 *  it's not high speed, it isn't slow and can handle reasonably demanding workloads.
 */
public class MiniStore
{
    private readonly string _path;

    public static void DeleteStore(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    public MiniStore(string path)
    {
        _path = path;
        EnsureDatabaseExists();
        // JournalModeWal();
    }

    private void JournalModeWal()
    {
        using var connection = Connection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode = WAL";
        command.ExecuteNonQuery();
    }

    private SqliteConnection Connection() => new($"Data Source={_path}");

    private void EnsureDatabaseExists()
    {
        using var connection = Connection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE IF NOT EXISTS Store (Key TEXT PRIMARY KEY, Data TEXT)";
        command.ExecuteNonQuery();
    }

    public void BatchPut(IEnumerable<(string key, string value)> items)
    {
        using var connection = Connection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR REPLACE INTO Store (Key, Data) VALUES (@key, @data)";
        command.Parameters.Add("@key", SqliteType.Text);
        command.Parameters.Add("@data", SqliteType.Text);
        foreach (var (key, value) in items)
        {
            command.Parameters["@key"].Value = key;
            command.Parameters["@data"].Value = value;
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public void Put(string key, string data)
    {
        using var connection = Connection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            $"INSERT INTO Store (Key, Data) VALUES (@key, @data) ON CONFLICT(Key) DO UPDATE SET Data = @data";
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@data", data);
        command.ExecuteNonQuery();
    }
    
    public void Put<T>(string key, T data)
    {
        using var connection = Connection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            $"INSERT INTO Store (Key, Data) VALUES (@key, @data) ON CONFLICT(Key) DO UPDATE SET Data = @data";
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@data", System.Text.Json.JsonSerializer.Serialize(data));
        command.ExecuteNonQuery();
    }
    public T? Get<T>(string key)
    {
        using var connection = Connection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT Data FROM Store WHERE Key = @key";
        command.Parameters.AddWithValue("@key", key);
        using var reader = command.ExecuteReader();
        return reader.Read() ? System.Text.Json.JsonSerializer.Deserialize<T>(reader.GetString(0)) : default;
    }

    public string Get(string key)
    {
        using var connection = Connection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT Data FROM Store WHERE Key = @key";
        command.Parameters.AddWithValue("@key", key);
        using var reader = command.ExecuteReader();
        return reader.Read() ? reader.GetString(0) : "";
    }

    public void Delete(string key)
    {
        using var connection = Connection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM Store WHERE Key = @key";
        command.Parameters.AddWithValue("@key", key);
        command.ExecuteNonQuery();
    }

    public bool Exists(string key)
    {
        using var connection = Connection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM Store WHERE Key = @key";
        command.Parameters.AddWithValue("@key", key);
        using var reader = command.ExecuteReader();
        var result = reader.Read() && reader.GetString(0) != "0";
        connection.Close();
        return result;
    }
    public List<string> Keys()
    {
        using var connection = Connection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Key FROM Store";
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        while(reader.Read())
        {
            result.Add(reader.GetString(0));
        }
        return result;
    }
}