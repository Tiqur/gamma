using System;
using System.Collections.Generic;
using System.Data.SQLite;

public class DatabaseSetup
{
    private const string ConnectionString = "Data Source=sample.db;Version=3;";

    public static void InitializeDatabase()
    {
        using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();

            // Create Cards table
            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Cards (
                    id INTEGER PRIMARY KEY,
                    front TEXT,
                    back TEXT,
                    tags TEXT
                )";
            using (SQLiteCommand cmd = new SQLiteCommand(createTableQuery, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static void SeedDatabase()
    {
        AddCard("Front 1", "Back 1", ["Tag1", "Tag2", "Tag3"]);
        AddCard("Front 2", "Back 2", ["Tag2", "Tag3"]);
        AddCard("Front 3", "Back 3", ["Tag1", "Tag2"]);
        AddCard("Front 4", "Back 4", ["Tag3"]);
    }

    public static void AddCard(string front, string back, List<string> tags)
    {
        using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();
            string insertQuery = "INSERT INTO Cards (front, back, tags) VALUES (@Front, @Back, @Tags)";
            using (SQLiteCommand cmd = new SQLiteCommand(insertQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Front", front);
                cmd.Parameters.AddWithValue("@Back", back);
                cmd.Parameters.AddWithValue("@Tags", string.Join(",", tags)); // Assuming tags are stored as comma-separated values
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static List<Card> GetAllCards()
    {
        List<Card> cards = new List<Card>();
        using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();
            string selectQuery = "SELECT id, front, back, tags FROM Cards";
            using (SQLiteCommand cmd = new SQLiteCommand(selectQuery, conn))
            {
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string front = reader.GetString(1);
                        string back = reader.GetString(2);
                        string tags = reader.GetString(3);
                        List<string> tagList = new List<string>(tags.Split(',')); // Assuming tags are stored as comma-separated values
                        cards.Add(new Card { Id = id, Front = front, Back = back, Tags = tagList });
                    }
                }
            }
        }
        return cards;
    }

    public static void UpdateCard(int id, string front, string back, List<string> tags)
    {
        using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();
            string updateQuery = "UPDATE Cards SET front = @Front, back = @Back, tags = @Tags WHERE id = @Id";
            using (SQLiteCommand cmd = new SQLiteCommand(updateQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Front", front);
                cmd.Parameters.AddWithValue("@Back", back);
                cmd.Parameters.AddWithValue("@Tags", string.Join(",", tags)); // Assuming tags are stored as comma-separated values
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static void DeleteCard(int id)
    {
        using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();
            string deleteQuery = "DELETE FROM Cards WHERE id = @Id";
            using (SQLiteCommand cmd = new SQLiteCommand(deleteQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }
        }
    }
}

public class Card
{
    public int Id { get; set; }
    public string Front { get; set; }
    public string Back { get; set; }
    public List<string> Tags { get; set; }
}

