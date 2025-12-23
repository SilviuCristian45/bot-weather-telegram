using System;
using System.Net.Http;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types; // Necesar pentru InputFile
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace WeatherBot
{
    class Program
    {
        // --- CONFIGURARE ---
        // Pune aici cheia nouă de la weatherapi.com
        private static  string WeatherApiKey = ""; 
        
        // Tokenul botului Telegram (rămâne același)
        private static  string TelegramBotToken = ""; 
        
        // ID-ul tău pe care l-am aflat (8092506115)
        private static  long TelegramChatId ;
        // -------------------

        static async Task Main(string[] args)
        {
            Console.WriteLine("Preluare date meteo (WeatherAPI)...");

            if (!LoadConfiguration())
            {
                return; // Oprim tot dacă nu găsim fisierul
            }
            try
            {
                // 1. Obținem datele
                var vremeBucuresti = await GetWeatherData("Bucharest");
                var vremeTurda = await GetWeatherData("Turda");

                // 2. Construim mesajul
                string mesajBucuresti = $"🌍 **Vremea in Bucuresti:**\n" +
                               $"🌡️ Temperatura: {vremeBucuresti.Temperatura}°C\n" +
                               $"☁️ Descriere: {vremeBucuresti.Descriere}\n";

                string mesajTurda =
                               $"🌍 **Vremea in Turda:**\n" +
                               $"🌡️ Temperatura: {vremeTurda.Temperatura}°C\n" +
                               $"☁️ Descriere: {vremeTurda.Descriere}\n";

                Console.WriteLine(mesajBucuresti);
                Console.WriteLine(mesajTurda);

                var botClient = new TelegramBotClient(TelegramBotToken);

                await botClient.SendMessage(
                    chatId: TelegramChatId,
                    text: "--------------------------------------------"
                );
               
                await botClient.SendMessage(
                    chatId: TelegramChatId,
                    text: String.Format("{0:f}",   DateTime.Now)
                );

                await botClient.SendMessage(
                    chatId: TelegramChatId,
                    text: mesajBucuresti
                );

                await botClient.SendMessage(
                    chatId: TelegramChatId,
                    parseMode: ParseMode.Markdown,
                    text: mesajTurda
                );
                
                Console.WriteLine("Mesaj trimis cu succes!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"A aparut o eroare: {ex.Message}");
            }
        }

        private static Boolean LoadConfiguration() {
            try
            {
                // Construim calea absolută către fișier (sigur pentru Cron/Linux)
                string caleFisier = Path.Combine(AppContext.BaseDirectory, "secrets.json");

                if (!File.Exists(caleFisier))
                {
                    Console.WriteLine($"EROARE: Nu gasesc fisierul de configurare la: {caleFisier}");
                    return false;
                }

                string continut = File.ReadAllText(caleFisier);
                var json = JsonSerializer.Deserialize<EnvVariables>(continut);
                
                if (json == null) {
                    Console.WriteLine($"EROARE la PARSARE: Fisierul nu e in formatul corect");
                    return false;
                }
                WeatherApiKey = json.WeatherApiKey;
                TelegramBotToken = json.TelegramBotToken;
                TelegramChatId = (long)json.TelegramChatId;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Nu am putut citi configurarea: {ex.Message}");
                return false;
            }
        }
        private static async Task<(string Descriere, string Temperatura, string image)> GetWeatherData(string oras)
        {
            bool weatherComputedSuccess = false;
            int step = 1;

            while (weatherComputedSuccess == false && step <= 5) {

                Console.WriteLine($"Weather api call try #{step}");
                 using (var client = new HttpClient())
                {
                    string url = $"http://api.weatherapi.com/v1/current.json?key={WeatherApiKey}&q={oras}&lang=ro";

                    var response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode) {
                        Console.WriteLine(response);
                        var json = await response.Content.ReadAsStringAsync();
                        WeatherResponse weatherForecast = JsonSerializer.Deserialize<WeatherResponse>(json)!;
                       
                        string descriere =  weatherForecast.Current.Condition.Text;
                        string temperatura = weatherForecast.Current.TempC.ToString();
                        string image = weatherForecast.Current.Condition.Icon;

                        if (image.StartsWith("//"))
                        {
                            image = "https:" + image;
                        }
                        weatherComputedSuccess = true;
                        return (descriere, temperatura, image);
                    }   
                }
                step++;
                Console.WriteLine("Wait 3 seconds ...");
                await Task.Delay(3000);;
            }
            throw new Exception("weather api calls failed for 5 times");
        }
    }
    
}