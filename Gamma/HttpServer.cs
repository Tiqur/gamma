using System;
using System.IO;
using System.Net;
using System.Text;
using System.Data.SQLite;
using System.Collections.Specialized;

public class HttpServer
{
    private readonly HttpListener _listener = new HttpListener();
    private const string ConnectionString = "Data Source=data.db;Version=3;";
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

    private string EscapeJsonString(string input)
    {
        if (input == null)
            return null;

        StringBuilder sb = new StringBuilder();
        foreach (char c in input)
        {
            switch (c)
            {
                case '\"':
                    sb.Append("\\\"");
                    break;
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (char.IsControl(c))
                    {
                        sb.Append(string.Format("\\u{0:X4}", (int)c));
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }

    private void HandleDataEndpoint(HttpListenerContext context)
    {
        NameValueCollection queryString = context.Request.QueryString;
        if (context.Request.HttpMethod == "GET")
        {
            List<Card> cards = DatabaseSetup.GetAllCards();

            // Create JSON manually (without using third-party libraries)
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach (var card in cards)
            {
                sb.Append("{");
                sb.Append($"\"id\": {card.Id}, ");
                sb.Append($"\"front\": \"{EscapeJsonString(card.Front)}\", ");
                sb.Append($"\"back\": \"{EscapeJsonString(card.Back)}\", ");
                sb.Append($"\"tag\": \"{EscapeJsonString(card.Tag)}\" ");
                sb.Append("}");

                if (card != cards[cards.Count - 1])
                    sb.Append(", ");
            }
            sb.Append("]");

            // Send JSON response
            WriteResponse(context, sb.ToString(), "application/json");
        }
        else if (context.Request.HttpMethod == "POST")
        {
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                string requestBody = reader.ReadToEnd();
                NameValueCollection postParams = System.Web.HttpUtility.ParseQueryString(requestBody);

                string front = postParams["front"];
                string back = postParams["back"];
                string tag = postParams["tag"];

                if (!string.IsNullOrEmpty(front) && !string.IsNullOrEmpty(back))
                {
                    DatabaseSetup.AddCard(front, back, tag);
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
                    string tag = putParams["tag"];

                    if (!string.IsNullOrEmpty(front) && !string.IsNullOrEmpty(back))
                    {
                        DatabaseSetup.UpdateCard(id, front, back, tag);
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
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                string requestBody = reader.ReadToEnd();
                NameValueCollection postParams = System.Web.HttpUtility.ParseQueryString(requestBody);

                if (int.TryParse(postParams["id"], out int id))
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
        }
        else
        {
            context.Response.StatusCode = 405;
            WriteResponse(context, "<html><body><h1>405 - Method Not Allowed</h1></body></html>");
        }
    }
    private void HandleRegenSeedEndpoint(HttpListenerContext context)
    {
        if (context.Request.HttpMethod == "GET")
        {
            // Generate new seed
            seed = Guid.NewGuid().GetHashCode();

            // Respond with success message
            context.Response.StatusCode = 200;
            WriteResponse(context, "<html><body><h1>200 - Seed Successfully Regenerated</h1></body></html>");
        }
        else
        {
            context.Response.StatusCode = 405;
            WriteResponse(context, "<html><body><h1>405 - Method Not Allowed</h1></body></html>");
        }
    }

    private void WriteResponse(HttpListenerContext context, string responseString, string contentType = "text/html")
    {
        context.Response.ContentType = contentType;
        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        context.Response.ContentLength64 = buffer.Length;
        Stream output = context.Response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
    }

    private void HandleCardEndpoint(HttpListenerContext context)
    {
        if (context.Request.HttpMethod == "GET")
        {
            string tag = context.Request.QueryString["tag"];
            
            if (!string.IsNullOrEmpty(tag))
            {
                List<Card> cards = DatabaseSetup.GetCardsByTag(tag);

                if (cards.Count > 0)
                {
                    // Select a random card
                    Random random = new Random(seed);
                    Card randomCard = cards[random.Next(cards.Count)];

                    // Create JSON for the random card
                    StringBuilder sb = new StringBuilder();
                    sb.Append("{");
                    sb.Append($"\"id\": {randomCard.Id}, ");
                    sb.Append($"\"front\": \"{EscapeJsonString(randomCard.Front)}\", ");
                    sb.Append($"\"back\": \"{EscapeJsonString(randomCard.Back)}\", ");
                    sb.Append($"\"tag\": \"{EscapeJsonString(randomCard.Tag)}\" ");
                    sb.Append("}");

                    // Send JSON response
                    WriteResponse(context, sb.ToString(), "application/json");
                }
                else
                {
                    context.Response.StatusCode = 404;
                    WriteResponse(context, "<html><body><h1>404 - Not Found. No cards found for the specified tag.</h1></body></html>");
                }
            }
            else
            {
                context.Response.StatusCode = 400;
                WriteResponse(context, "<html><body><h1>400 - Bad Request. Tag parameter is required.</h1></body></html>");
            }
        }
        else
        {
            context.Response.StatusCode = 405;
            WriteResponse(context, "<html><body><h1>405 - Method Not Allowed</h1></body></html>");
        }
    }
    private void ProcessRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        // Set CORS headers
        response.AddHeader("Access-Control-Allow-Origin", "*");
        response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");

        // Handle OPTIONS requests (CORS preflight)
        if (request.HttpMethod == "OPTIONS")
        {
          response.StatusCode = (int)HttpStatusCode.OK;
          response.Close();
          return;
        }


        switch (context.Request.Url.AbsolutePath)
        {
            case "/":
                ServeIndexHtml(context);
                break;
            case "/card":
                HandleCardEndpoint(context);
                break;
            case "/data":
                HandleDataEndpoint(context);
                break;
            case "/seed":
                HandleRegenSeedEndpoint(context);
                break;
            default:
                WriteResponse(context, "<html><body><h1>404 - Not Found</h1></body></html>");
                break;
        }
    }
    private void ServeIndexHtml(HttpListenerContext context)
    {
        try
        {
            string projectRoot = Directory.GetCurrentDirectory();
            string indexPath = Path.Combine(projectRoot, "public", "index.html");

            if (File.Exists(indexPath))
            {
                string htmlContent = File.ReadAllText(indexPath);
                WriteResponse(context, htmlContent);
            }
            else
            {
                context.Response.StatusCode = 404;
                WriteResponse(context, "<html><body><h1>404 - File Not Found</h1></body></html>");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error serving index.html: " + ex.Message);
            context.Response.StatusCode = 500;
            WriteResponse(context, "<html><body><h1>500 - Internal Server Error</h1></body></html>");
        }
    }

    public void Stop()
    {
        _listener.Stop();
        _listener.Close();
    }
}

