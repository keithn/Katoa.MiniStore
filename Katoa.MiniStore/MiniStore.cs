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
    private readonly Options _options;

    public static void DeleteStore(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    /// <summary>
    /// Allows a custom connection string and allows you to send the sqlite DB any commands before it's used.  This is
    /// useful if you need to send a pragma command to the DB before it's used.
    /// </summary>
    public MiniStore(Options options)
    {
        _options = options;
        EnsureDatabaseExists();
        DoPreCommands();
    }

    /// <summary>
    /// Most straightforward constructor which creates/opens a Db at the given path
    /// </summary>
    /// <param name="path"></param>
    public MiniStore(string path) : this(new Options().FromPath(path))
    {
    }

    private void DoPreCommands()
    {
        using var connection = Connection();
        connection.Open();
        using var command = connection.CreateCommand();
        foreach (var sql in _options.PreCommands)
        {
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }

    private SqliteConnection Connection() => new(_options.InternalConnectionString);

    private void EnsureDatabaseExists()
    {
        using var connection = Connection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE IF NOT EXISTS Store (Key TEXT PRIMARY KEY, Data TEXT)";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts a lot of values at once. This is much much faster than doing individual puts.
    /// </summary>
    /// <param name="items"></param>
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
/// <summary>
/// Put a single key value.  This either creates or updates the value.
/// </summary>
/// <param name="key"></param>
/// <param name="data"></param>
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

/// <summary>
/// Puts a .NET object by serializing it to JSON and stores it against the given key
/// </summary>
/// <param name="key"></param>
/// <param name="data"></param>
/// <typeparam name="T"></typeparam>
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
/// <summary>
/// Gets .NET object by deserializing it from JSON given the key
/// </summary>
/// <param name="key"></param>
/// <typeparam name="T"></typeparam>
/// <returns></returns>
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

/// <summary>
/// Gets a string value given the key.  Returns an empty string if the key is not found.
/// </summary>
/// <param name="key"></param>
/// <returns></returns>
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

/// <summary>
/// Deletes the keyvalue from the store
/// </summary>
/// <param name="key"></param>
    public void Delete(string key)
    {
        using var connection = Connection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM Store WHERE Key = @key";
        command.Parameters.AddWithValue("@key", key);
        command.ExecuteNonQuery();
    }
/// <summary>
/// Checks if the given key exists in the store
/// </summary>
/// <param name="key"></param>
/// <returns></returns>
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
/// <summary>
/// Gets all the keys in the store
/// </summary>
/// <returns></returns>
    public List<string> Keys()
    {
        using var connection = Connection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Key FROM Store";
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        while (reader.Read())
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }
/// <summary>
/// Gets all the keys that match the given pattern (see SQLites LIKE syntax)
/// </summary>
/// <param name="expression"></param>
/// <returns></returns>
    public List<string> KeysLike(string expression)
    {
        using var connection = Connection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Key FROM Store WHERE Key LIKE @expression";
        command.Parameters.AddWithValue("@expression", expression);
        using var reader = command.ExecuteReader();
        var result = new List<string>();
        while (reader.Read())
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }
    public class Options
    {
        internal readonly List<string> PreCommands = new();
        internal string InternalConnectionString { get; set; } = "";

        public Options ConnectionString(string connectionString)
        {
            InternalConnectionString = connectionString;
            return this;
        }

        public Options FromPath(string path) => ConnectionString($"Data Source={path}");

        public Options PreCommand(string command)
        {
            PreCommands.Add(command);
            return this;
        }

        public Options JournalModeWal() => PreCommand("PRAGMA journal_mode = WAL");
    }
}