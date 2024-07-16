using System;
using System.IO;
using System.Net;
using System.Text;
using System.Data.SQLite;
using System.Collections.Specialized;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    private void ServeIndexHtml(HttpListenerContext context)
    {
        ServeStaticFile(context, "index.html", "text/html");
    }

    private void ServeMainCss(HttpListenerContext context)
    {
        ServeStaticFile(context, "main.css", "text/css");
    }

    private void ServeMainJs(HttpListenerContext context)
    {
        ServeStaticFile(context, "main.js", "text/javascript");
    }

    private void ServeStaticFile(HttpListenerContext context, string filename, string contentType)
    {
        try
        {
            string projectRoot = Directory.GetCurrentDirectory();
            string filePath = Path.Combine(projectRoot, "public", filename);

            if (File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(filePath);
                WriteResponse(context, fileContent, contentType);
            }
            else
            {
                context.Response.StatusCode = 404;
                WriteResponse(context, "<html><body><h1>404 - File Not Found</h1></body></html>");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error serving {filename}: " + ex.Message);
            context.Response.StatusCode = 500;
            WriteResponse(context, "<html><body><h1>500 - Internal Server Error</h1></body></html>");
        }
    }

    private async void ProcessRequest(HttpListenerContext context)
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
            case "/main.css":
                ServeMainCss(context);
                break;
            case "/main.js":
                ServeMainJs(context);
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
            case "/generate":
                await HandleGenerateEndpoint(context);
                break;
            default:
                context.Response.StatusCode = 404;
                WriteResponse(context, "<html><body><h1>404 - Not Found</h1></body></html>");
                break;
        }
    }

    private async Task<string> CallChatGptApi(string prompt, int count)
    {
        try
        {
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("API key for OpenAI is not set in the environment variables.");
            }

            string apiUrl = "https://api.openai.com/v1/chat/completions";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                      new {
                        role = "user",
                        content =
                        $"Using this prompt: \"{prompt}\" generate {count} math flashcards in this format:\n" +
                        @"```front
                        <Instructions>: <\[ tex equation ]\>
                        ```

                        ```back
                        <\[ tex equation ]\>
                        ```"
                      }
                    },
                    temperature = 0.7,
                    max_tokens = 100*count,
                    n = 1
                };

                string jsonRequestBody = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

                // Set Content-Type header on the HttpContent object
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await client.PostAsync(apiUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                return responseString;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling OpenAI API: {ex.Message}");
            throw; // Rethrow the exception to propagate it up
        }
    }


    private async Task<List<Card>> GenerateCards(string prompt, int count, string tag)
    {
        try
        {
            var responseString = await CallChatGptApi(prompt, count);

            if (string.IsNullOrEmpty(responseString))
            {
                Console.WriteLine("ChatGPT API returned an empty response.");
                return new List<Card>();
            }

            Console.WriteLine("API Response: " + responseString);

            var responseJson = JsonConvert.DeserializeObject<JObject>(responseString);
            var choicesArray = responseJson["choices"];

            if (choicesArray == null)
            {
                Console.WriteLine("No 'choices' array found in the API response.");
                return new List<Card>();
            }

            List<Card> generatedCards = new List<Card>();

            foreach (var choice in choicesArray)
            {
                var message = choice["message"];
                if (message == null)
                {
                    Console.WriteLine("No 'message' object found in the choice.");
                    continue;
                }

                var content = message["content"];
                if (content == null)
                {
                    Console.WriteLine("No 'content' found in the message.");
                    continue;
                }

                string contentString = content.ToString();

                // Split the content into multiple cards
                var cardsContent = SplitIntoCards(contentString);

                foreach (var cardContent in cardsContent)
                {
                    string front = ExtractContent(cardContent, "front");
                    string back = ExtractContent(cardContent, "back");

                    generatedCards.Add(new Card
                    {
                        Front = front,
                        Back = back,
                        Tag = tag
                    });
                }
            }

            return generatedCards;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating cards: {ex.Message}");
            return new List<Card>();
        }
    }

    private List<string> SplitIntoCards(string content)
    {
        var cards = new List<string>();
        var cardContents = Regex.Split(content, @"(?<=```back\n.*\n```)", RegexOptions.Singleline);

        foreach (var cardContent in cardContents)
        {
            if (!string.IsNullOrWhiteSpace(cardContent))
            {
                cards.Add(cardContent.Trim());
            }
        }

        return cards;
    }

    private string ExtractContent(string text, string section)
    {
        string pattern = $"{section}\n(.*)";
        Match match = Regex.Match(text, pattern);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        // Handle case where section is not found
        Console.WriteLine($"Error extracting {section} content from text: {text}");
        return "Content Not Found";
    }


    private async Task HandleGenerateEndpoint(HttpListenerContext context)
    {
        if (context.Request.HttpMethod == "GET")
        {
            string prompt = context.Request.QueryString["prompt"];
            string countString = context.Request.QueryString["count"];
            string tag = context.Request.QueryString["tag"];

            if (!string.IsNullOrEmpty(countString) && int.TryParse(countString, out int count))
            {
                if (!string.IsNullOrEmpty(prompt) || !string.IsNullOrEmpty(tag))
                {
                    List<Card> generatedCards = await GenerateCards(prompt, count, tag);

                    if (generatedCards != null && generatedCards.Count > 0)
                    {
                        // Create JSON for the generated cards
                        StringBuilder sb = new StringBuilder();
                        sb.Append("[");
                        foreach (var card in generatedCards)
                        {
                            sb.Append("{");
                            sb.Append($"\"id\": 0, ");  // No ID since they are not in the DB yet
                            sb.Append($"\"front\": \"{EscapeJsonString(card.Front)}\", ");
                            sb.Append($"\"back\": \"{EscapeJsonString(card.Back)}\", ");
                            sb.Append($"\"tag\": {card.Tag} ");
                            sb.Append("}");

                            if (card != generatedCards[generatedCards.Count - 1])
                                sb.Append(", ");
                        }
                        sb.Append("]");

                        // Send JSON response
                        WriteResponse(context, sb.ToString(), "application/json");
                        return;
                    }
                }
            }

            // If any conditions fail, return a 400 Bad Request
            context.Response.StatusCode = 400;
            WriteResponse(context, "<html><body><h1>400 - Bad Request. Prompt, tag, and count parameters are required.</h1></body></html>");
        }
        else
        {
            context.Response.StatusCode = 405;
            WriteResponse(context, "<html><body><h1>405 - Method Not Allowed</h1></body></html>");
        }
    }


    public void Stop()
    {
        _listener.Stop();
        _listener.Close();
    }
}

