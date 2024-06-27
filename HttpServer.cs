using System;
using System.IO;
using System.Net;
using System.Text;
using System.Data.SQLite;

public class HttpServer
{
    private readonly HttpListener _listener = new HttpListener();
    private const string ConnectionString = "Data Source=sample.db;Version=3;";

    public HttpServer(string[] prefixes)
    {
        if (!HttpListener.IsSupported)
        {
            throw new NotSupportedException("The HttpListener class is not supported on this platform.");
        }

        if (prefixes == null || prefixes.Length == 0)
        {
            throw new ArgumentException("At least one URI prefix is required.");
        }

        foreach (string prefix in prefixes)
        {
            _listener.Prefixes.Add(prefix);
        }

        _listener.Start();
    }

    public void Start()
    {
        Console.WriteLine("Listening...");
        while (true)
        {
            HttpListenerContext context = _listener.GetContext();
            ProcessRequest(context);
        }
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        string responseString = "";

        switch (context.Request.Url.AbsolutePath)
        {
            case "/":
                responseString = "<html><body><h1>Welcome to the Simple HTTP Server</h1></body></html>";
                break;
            case "/data":
                responseString = GetDatabaseData();
                break;
            default:
                responseString = "<html><body><h1>404 - Not Found</h1></body></html>";
                break;
        }

        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        context.Response.ContentLength64 = buffer.Length;
        Stream output = context.Response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
    }

    private string GetDatabaseData()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<html><body><h1>Data from SQLite Database</h1><ul>");

        using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();
            string query = "SELECT * FROM SampleTable";
            using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
            using (SQLiteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    sb.Append("<li>").Append(reader["Data"]).Append("</li>");
                }
            }
        }

        sb.Append("</ul></body></html>");
        return sb.ToString();
    }

    public void Stop()
    {
        _listener.Stop();
        _listener.Close();
    }
}

