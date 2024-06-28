using System;
using System.IO;
using System.Net;
using System.Text;
using System.Data.SQLite;
using System.Collections.Specialized;

public class HttpServer
{
    private readonly HttpListener _listener = new HttpListener();
    private const string ConnectionString = "Data Source=sample.db;Version=3;";
    private int seed = Guid.NewGuid().GetHashCode();

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

    private void HandleDataEndpoint(HttpListenerContext context)
    {
        NameValueCollection queryString = context.Request.QueryString;
        if (context.Request.HttpMethod == "GET")
        {
            foreach (string key in queryString.AllKeys)
            {
                string[] values = queryString.GetValues(key);
                if (values != null)
                {
                    foreach (string value in values)
                    {
                        Console.WriteLine($"Key: {key}, Value: {value}");
                    }
                }
            }

            string databaseData = GetDatabaseData();
            WriteResponse(context, databaseData);
        }
        else if (context.Request.HttpMethod == "POST")
        {
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                string requestBody = reader.ReadToEnd();
                NameValueCollection postParams = System.Web.HttpUtility.ParseQueryString(requestBody);

                string front = postParams["front"];
                string back = postParams["back"];
                string tagsString = postParams["tags"];
                List<string> tags = new List<string>(tagsString.Split(','));

                if (!string.IsNullOrEmpty(front) && !string.IsNullOrEmpty(back) && tags.Count > 0)
                {
                    DatabaseSetup.AddCard(front, back, tags);
                    context.Response.StatusCode = 200;
                    WriteResponse(context, "<html><body><h1>Card added successfully!</h1></body></html>");
                }
                else
                {
                    context.Response.StatusCode = 400;
                    WriteResponse(context, "<html><body><h1>400 - Bad Request. All fields are required.</h1></body></html>");
                }
            }
        }
        else if (context.Request.HttpMethod == "PUT")
        {
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                string requestBody = reader.ReadToEnd();
                NameValueCollection putParams = System.Web.HttpUtility.ParseQueryString(requestBody);

                if (int.TryParse(putParams["id"], out int id))
                {
                    string front = putParams["front"];
                    string back = putParams["back"];
                    string tagsString = putParams["tags"];
                    List<string> tags = new List<string>(tagsString.Split(','));

                    if (!string.IsNullOrEmpty(front) && !string.IsNullOrEmpty(back) && tags.Count > 0)
                    {
                        DatabaseSetup.UpdateCard(id, front, back, tags);
                        context.Response.StatusCode = 200;
                        WriteResponse(context, "<html><body><h1>Card updated successfully!</h1></body></html>");
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        WriteResponse(context, "<html><body><h1>400 - Bad Request. All fields are required.</h1></body></html>");
                    }
                }
                else
                {
                    context.Response.StatusCode = 400;
                    WriteResponse(context, "<html><body><h1>400 - Bad Request. Invalid ID.</h1></body></html>");
                }
            }
        }
        else if (context.Request.HttpMethod == "DELETE")
        {
            NameValueCollection queryStringParams = context.Request.QueryString;

            if (int.TryParse(queryStringParams["id"], out int id))
            {
                DatabaseSetup.DeleteCard(id);
                context.Response.StatusCode = 200;
                WriteResponse(context, "<html><body><h1>Card deleted successfully!</h1></body></html>");
            }
            else
            {
                context.Response.StatusCode = 400;
                WriteResponse(context, "<html><body><h1>400 - Bad Request. Invalid ID.</h1></body></html>");
            }
        }
        else
        {
            context.Response.StatusCode = 405;
            WriteResponse(context, "<html><body><h1>405 - Method Not Allowed</h1></body></html>");
        }
    }
    private void HandleRegenSeedEndpoint(HttpListenerContext context)
    {
        if (context.Request.HttpMethod == "POST")
        {
            context.Response.StatusCode = 200;
            seed = Guid.NewGuid().GetHashCode();
        }
        else
        {
            context.Response.StatusCode = 405;
            WriteResponse(context, "<html><body><h1>405 - Method Not Allowed</h1></body></html>");
        }
    }

    private void WriteResponse(HttpListenerContext context, string responseString)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        context.Response.ContentLength64 = buffer.Length;
        Stream output = context.Response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        switch (context.Request.Url.AbsolutePath)
        {
            case "/":
                WriteResponse(context, "<html><body><h1>Welcome to the Simple HTTP Server</h1></body></html>");
                break;
            case "/data":
                HandleDataEndpoint(context);
                break;
            case "/regen_seed":
                HandleRegenSeedEndpoint(context);
                break;
            default:
                WriteResponse(context, "<html><body><h1>404 - Not Found</h1></body></html>");
                break;
        }
    }
    private string GetDatabaseData()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<html><body><h1>Data from SQLite Database</h1><ul>");

        using (SQLiteConnection conn = new SQLiteConnection(ConnectionString))
        {
            conn.Open();
            string query = "SELECT id, front, back, tags FROM Cards";
            using (SQLiteCommand cmd = new SQLiteCommand(query, conn))
            using (SQLiteDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string front = reader.GetString(1);
                    string back = reader.GetString(2);
                    string tags = reader.GetString(3);

                    sb.Append("<li>")
                      .Append("<strong>ID:</strong> ").Append(id).Append("<br>")
                      .Append("<strong>Front:</strong> ").Append(front).Append("<br>")
                      .Append("<strong>Back:</strong> ").Append(back).Append("<br>")
                      .Append("<strong>Tags:</strong> ").Append(tags)
                      .Append("</li><br>");
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

