using System;

class Program
{
    static void Main(string[] args)
    {
        DatabaseSetup.InitializeDatabase();
        DatabaseSetup.SeedDatabase();

        string[] prefixes = { "http://localhost:3001/" };
        HttpServer server = new HttpServer(prefixes);

        server.Start();
    }
}

