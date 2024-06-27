using System;
using System.Data.SQLite;

public class DatabaseSetup
{
    private const string ConnectionString = "Data Source=sample.db;Version=3;";

    public static void InitializeDatabase()
    {
        using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();
            string createTableQuery = "CREATE TABLE IF NOT EXISTS SampleTable (Id INTEGER PRIMARY KEY, Data TEXT)";
            using (SQLiteCommand cmd = new SQLiteCommand(createTableQuery, conn))
            {
                cmd.ExecuteNonQuery();
            }

            string insertDataQuery = "INSERT INTO SampleTable (Data) VALUES ('Sample Data 1'), ('Sample Data 2')";
            using (SQLiteCommand cmd = new SQLiteCommand(insertDataQuery, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
}

