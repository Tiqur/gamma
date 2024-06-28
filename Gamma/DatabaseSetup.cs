using System;
using System.Collections.Generic;
using System.Data.SQLite;

public class DatabaseSetup
{
    private const string ConnectionString = "Data Source=data.db;Version=3;";

    public static void InitializeDatabase()
    {
        using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();

            // Create Cards table with a single tag column
            string createTableQuery = @"
                CREATE TABLE IF NOT EXISTS Cards (
                    id INTEGER PRIMARY KEY,
                    front TEXT,
                    back TEXT,
                    tag TEXT
                )";
            using (SQLiteCommand cmd = new SQLiteCommand(createTableQuery, conn))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }

    public static void SeedDatabase()
    {
        Random random = new Random();

        for (int i = 0; i < 1000; i++)
        {
            int front = random.Next(1, 10000000);
            int back = random.Next(1, 10000000);

            string frontText = $"Front {i+1}";
            string backText = $"Back {i+1}";

            AddCard(frontText, backText, $"Tag{i%10}");
        }
    }

    public static void AddCard(string front, string back, string tag)
    {
        using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();
            string insertQuery = "INSERT INTO Cards (front, back, tag) VALUES (@Front, @Back, @Tag)";
            using (SQLiteCommand cmd = new SQLiteCommand(insertQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Front", front);
                cmd.Parameters.AddWithValue("@Back", back);
                cmd.Parameters.AddWithValue("@Tag", tag);
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
            string selectQuery = "SELECT id, front, back, tag FROM Cards";
            using (SQLiteCommand cmd = new SQLiteCommand(selectQuery, conn))
            {
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string front = reader.GetString(1);
                        string back = reader.GetString(2);
                        string tag = reader.GetString(3);
                        cards.Add(new Card { Id = id, Front = front, Back = back, Tag = tag });
                    }
                }
            }
        }
        return cards;
    }

    public static List<Card> GetCardsByTag(string tag)
    {
        List<Card> cards = new List<Card>();
        using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();
            string selectQuery = "SELECT id, front, back, tag FROM Cards WHERE tag = @Tag";
            using (SQLiteCommand cmd = new SQLiteCommand(selectQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Tag", tag);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = reader.GetInt32(0);
                        string front = reader.GetString(1);
                        string back = reader.GetString(2);
                        string retrievedTag = reader.GetString(3);
                        cards.Add(new Card { Id = id, Front = front, Back = back, Tag = retrievedTag });
                    }
                }
            }
        }
        return cards;
    }

    public static void UpdateCard(int id, string front, string back, string tag)
    {
        using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();
            string updateQuery = "UPDATE Cards SET front = @Front, back = @Back, tag = @Tag WHERE id = @Id";
            using (SQLiteCommand cmd = new SQLiteCommand(updateQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Front", front);
                cmd.Parameters.AddWithValue("@Back", back);
                cmd.Parameters.AddWithValue("@Tag", tag);
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
    public string Tag { get; set; }
}

