using System;

class Program
{
    static void Main(string[] args)
    {
        DatabaseSetup.InitializeDatabase();
        //DatabaseSetup.SeedDatabase();

        string[] prefixes = { "http://+:2205/" };
        HttpServer server = new HttpServer(prefixes);

        server.Start();
    }
}

