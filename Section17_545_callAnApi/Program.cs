using System.Text.Json;

namespace Section17_545_callAnApi;

internal class Program
{
    private static string _baseUrl = "https://api.gameofthronesquotes.xyz/";
    private static string _version = "v1/";
    private static string _call = "random/";
    private static int _amount = 5;
    private static List<Quote> quotes = new();

    // sometimes it times out, sometimes it doesn't
    // depending on how tired we have become of waiting...
    private static CancellationTokenSource cancellationTokenSource = new();
    private static int wantedTimeOut = new Random().Next(100,600);


    static void Main(string[] args)
    {
        string toCall = _baseUrl + _version + _call + _amount;
        var task = CreateQuotesAsync(toCall, cancellationTokenSource);

        Console.WriteLine("back in main waiting for the task to complete");

        var someTaskThatTimesOut = BecomeTiredOfWaitingAfter(wantedTimeOut);

        do
        {
            Console.WriteLine("In main, just waiting and showing off that I'm still responsive.");
            Thread.Sleep(100);
        } while (quotes.Count == 0);


        foreach (Quote quote in quotes)
        {
            Console.WriteLine($"Said: {quote.sentence}");
            Console.WriteLine("by: " + quote.character.name);
            Console.WriteLine("of house: " + quote.character.house.name);
        }

        Console.WriteLine("Press enter to finish this silly game of Cat & Mouse");
        Console.ReadLine();
    }

    static async Task BecomeTiredOfWaitingAfter(int milliSeconds)
    {
        await Task.Delay(milliSeconds);
        cancellationTokenSource.Cancel();
    }
    static async Task CreateQuotesAsync(string url, CancellationTokenSource cancellationTokenSource)
    {
        string jsonString = "{\"sentence\": \"Something went wrong while working, but I wanted to still say: I drink and I know things.\",\"character\": {\"name\": \"Tyrion Lannister\",\"slug\": \"Tyrion\",\"house\": {\"name\": \"House Tarly of Horn Hill\",\"slug\": \"tarly\"}}}";
        try
        {
            jsonString = await GetQuotesStringAsync(url,cancellationTokenSource);
        }
        catch (HttpIOException ioex)
        {
            Console.WriteLine("There was trouble getting the data online. " + ioex.Message);
            Console.WriteLine("Therefore I am now reverting to a standard string with only one single quote");
            quotes.Add(JsonSerializer.Deserialize<Quote>(jsonString));
            throw;
        }
        catch(TaskCanceledException tex)
        {
            Console.WriteLine("You had the audacity to cancel the HttpRequest! " + tex.Message);
            quotes.Add(JsonSerializer.Deserialize<Quote>(jsonString));
            throw;
        }
        catch(Exception ex)
        {
            Console.WriteLine("Unexpected error: " + ex.Message + "\n" + ex);
            quotes.Add(JsonSerializer.Deserialize<Quote>(jsonString));
            throw;
        }

        try
        {
            foreach (Quote quote in JsonSerializer.Deserialize<List<Quote>>(jsonString)!)
            {
                quotes.Add(quote);
            }
        }
        catch(JsonException ex)
        {
            Console.WriteLine("There is something wrong with the Deserialization: " + ex.Message);
            throw;
        }
    }

    static async Task<string> GetQuotesStringAsync(string url, CancellationTokenSource cancellationTokenSource)
    {
        using (HttpClient httpClient = new HttpClient())
        {
            HttpResponseMessage httpResponse = await httpClient.GetAsync(url,cancellationTokenSource.Token);
            httpResponse.EnsureSuccessStatusCode();
            //throw new HttpIOException(HttpRequestError.Unknown, "manually throwing a HttpIOException for testing");

            return await httpResponse.Content.ReadAsStringAsync();
        }
    }
}

public record Character(
    string name,
    string slug,
    House house
);

public record House(
    string name,
    string slug
);

public record Quote(
    string sentence,
    Character character
);
