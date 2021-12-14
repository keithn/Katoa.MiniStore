using Microsoft.Data.Sqlite;

namespace Katoa.MiniStore;

public class MiniStore
{
    private readonly string _path;

    public MiniStore(string path)
    {
        _path = path;
        EnsureDatabaseExists();
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
    public void Put(string key, string data)
    {
        using var connection = Connection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"INSERT INTO Store (Key, Data) VALUES (@key, @data) ON CONFLICT(Key) DO UPDATE SET Data = @data";
        command.Parameters.AddWithValue("@key", key);
        command.Parameters.AddWithValue("@data", data);
        command.ExecuteNonQuery();
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
        return reader.Read() && reader.GetString(0) != "0";
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