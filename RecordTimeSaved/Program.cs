using System.Globalization;
using Microsoft.Data.Sqlite;

namespace RecordTimeSaved;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Usage();
            return;
        }
        var mode = args[0].ToLowerInvariant();
        if (mode == "read")
        {
            ReadMode(args);
        }
        else if (mode == "record")
        {
            RecordMode(args);
        }
        else
        {
            Usage();
        }
    }
    private static string GetDbFilePath()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TimeSaved" , "TimeSaved.db");
    }

    private static void RecordMode(string[] args)
    {
        if (args.Length != 3)
        {
            Usage();
            return;
        }
        var tool = args[1];
        if (!int.TryParse(args[2], out var timeInSeconds) || timeInSeconds < 0)
        {
            Console.WriteLine("Invalid time in seconds. It must be a non-negative integer.");
            return;
        }
        
        using var connection = CreateConnection();
        RecordTimeSaved(connection, tool, timeInSeconds);
        connection.Close();
    }


    private static void ReadMode(string[] args)
    {
        using var connection = CreateConnection();
        ReadTimeSaved(connection);
        connection.Close();
    }
    
    private static SqliteConnection CreateConnection()
    {
        SqliteConnection? connection = null;
        try
        {
            var dbPath = GetDbFilePath();
            if (!Directory.Exists(Path.GetDirectoryName(dbPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            }
            var connectionString = $"Data Source={dbPath}";
            connection = new SqliteConnection(connectionString);
            connection.Open();
            if (!IsDbSetup(connection))
            {
                SetupDb(connection);
            }

            return connection;
        }
        catch
        {
            connection?.Dispose();
            throw;
        }
    }

    static void Usage()
    {
        Console.WriteLine("Usage: RecordTimeSaved <mode>");
        Console.WriteLine("Where <mode> is one of the following:");
        Console.WriteLine("record");
        Console.WriteLine("read");
        Console.WriteLine("Record usage: RecordTimeSaved record <tool> <timeInSeconds>");
        Console.WriteLine("Read usage: RecordTimeSaved read");
    }
    
    static void ReadTimeSaved(SqliteConnection connection)
    {
        var timeSaved = GetTimeSaved(connection);
        var earliestTimeSaved = GetEarliestTimeSaved(connection);
        if (earliestTimeSaved == null)
        {
            Console.WriteLine("No time saved records found.");
            return;
        }
        
        Console.WriteLine($"Time saved by tool since {earliestTimeSaved:d}:");
        foreach (var entry in timeSaved)
        {
            Console.WriteLine($"{entry.Key}: {HumanizeTime(entry.Value)}");
        }
    }
    
    static void RecordTimeSaved(SqliteConnection connection, string tool, int timeInSeconds)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO TimeSaved (Tool, TimeInSeconds) VALUES (@tool, @timeInSeconds)";
        command.Parameters.AddWithValue("@tool", tool);
        command.Parameters.AddWithValue("@timeInSeconds", timeInSeconds);
        command.ExecuteNonQuery();
    }

    static void SetupDb(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS TimeSaved (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Tool TEXT NOT NULL,
                TimeInSeconds INTEGER NOT NULL,
                CreatedOn DATETIME DEFAULT CURRENT_TIMESTAMP
            )";
        command.ExecuteNonQuery();
    }
    static DateTime? GetEarliestTimeSaved(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT MIN(CreatedOn) FROM TimeSaved";
        var result = command.ExecuteScalar();
        if (result == null || result == DBNull.Value)
        {
            return null;
        }

        if (DateTime.TryParse(result?.ToString(), out DateTime earliestTime))
        {
            return earliestTime;
        }
        return null;
    }
    static bool IsDbSetup(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='TimeSaved'";
        using var reader = command.ExecuteReader();
        return reader.Read();
    }
    static Dictionary<string,int> GetTimeSaved(SqliteConnection connection)
    {
        var timeSaved = new Dictionary<string, int>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Tool, SUM(TimeInSeconds) FROM TimeSaved GROUP BY Tool";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var tool = reader.GetString(0);
            var timeInSeconds = reader.GetInt32(1);
            timeSaved[tool] = timeInSeconds;
        }
        return timeSaved;
    }
    static string HumanizeTime(int seconds)
    {
        if (seconds < 60)
            return $"{seconds} seconds";
        if (seconds < 3600)
            return $"{seconds / 60} minutes";
        if (seconds < 86400)
            return $"{seconds / 3600} hours";
        return $"{seconds / 86400} days";
    }
}