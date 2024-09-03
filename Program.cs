using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private static readonly string functionBaseUrl = "";

    static async Task Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("Treasure Hunt Game");
            Console.WriteLine("1. Submit Treasure");
            Console.WriteLine("2. View Leaderboard");
            Console.WriteLine("3. Get Treasure Hint");
            Console.WriteLine("4. Exit");
            Console.Write("Choose an option: ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await SubmitTreasure();
                    break;
                case "2":
                    await ViewLeaderboard();
                    break;
                case "3":
                    await GetTreasureHint();
                    break;
                case "4":
                    return;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    break;
            }

            Console.WriteLine();
        }
    }

    static async Task SubmitTreasure()
    {
        Console.Write("Enter team name: ");
        string teamName = Console.ReadLine();
        Console.Write("Enter treasure ID: ");
        string treasureId = Console.ReadLine();

        var content = new StringContent(JsonConvert.SerializeObject(new { teamName, treasureId }), Encoding.UTF8, "application/json");
        var response = await client.PostAsync($"{functionBaseUrl}/api/SubmitTreasure", content);

        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Treasure submitted successfully!");
        }
        else
        {
            Console.WriteLine($"Error: {await response.Content.ReadAsStringAsync()}");
        }
    }

    static async Task ViewLeaderboard()
    {
        var response = await client.GetAsync($"{functionBaseUrl}/api/GetLeaderboard");

        if (response.IsSuccessStatusCode)
        {
            var leaderboard = JsonConvert.DeserializeObject<List<dynamic>>(await response.Content.ReadAsStringAsync());
            Console.WriteLine("Leaderboard:");
            foreach (var entry in leaderboard)
            {
                Console.WriteLine($"Team: {entry.TeamName}, Treasures Found: {entry.TreasuresFound}, Last Submission: {entry.LastSubmission}");
            }
        }
        else
        {
            Console.WriteLine($"Error: {await response.Content.ReadAsStringAsync()}");
        }
    }

    static async Task GetTreasureHint()
    {
        Console.Write("Enter treasure ID: ");
        string treasureId = Console.ReadLine();

        var response = await client.GetAsync($"{functionBaseUrl}/api/GetTreasureHint?treasureId={treasureId}");

        if (response.IsSuccessStatusCode)
        {
            string hint = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Hint: {hint}");
        }
        else
        {
            Console.WriteLine($"Error: {await response.Content.ReadAsStringAsync()}");
        }
    }
}
