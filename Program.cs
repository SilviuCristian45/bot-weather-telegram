using System;
using System.Net.Http;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types; // Necesar pentru InputFile
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json.Linq;

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
                               $"☁️ Descriere: {vremeBucuresti.Descriere}\n\n" + 
                               $"Imagine: []({vremeBucuresti.image})";

                string mesajTurda =
                               $"🌍 **Vremea in Turda:**\n" +
                               $"🌡️ Temperatura: {vremeTurda.Temperatura}°C\n" +
                               $"☁️ Descriere: {vremeTurda.Descriere}\n" + 
                               $"Imagine: []({vremeTurda.image}) ";

                Console.WriteLine(mesajBucuresti);
                Console.WriteLine(mesajTurda);

                var botClient = new TelegramBotClient(TelegramBotToken);

                await botClient.SendPhoto(
                    chatId: TelegramChatId,
                    photo: InputFile.FromUri(vremeBucuresti.image), // Aici punem URL-ul imaginii
                    caption: mesajBucuresti,
                    parseMode: ParseMode.Markdown
                );

                await botClient.SendPhoto(
                    chatId: TelegramChatId,
                    parseMode: ParseMode.Markdown,
                    photo: InputFile.FromUri(vremeTurda.image), // Aici punem URL-ul imaginii
                    caption: mesajTurda
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
                var json = JObject.Parse(continut);

                WeatherApiKey = json["WeatherApiKey"].ToString();
                TelegramBotToken = json["TelegramBotToken"].ToString();
                TelegramChatId = (long)json["TelegramChatId"];

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
            using (var client = new HttpClient())
            {
                string url = $"http://api.weatherapi.com/v1/current.json?key={WeatherApiKey}&q={oras}&lang=ro";

                var response = await client.GetStringAsync(url);

                Console.WriteLine(response);
                var json = JObject.Parse(response);

                // Extragem datele specifice structurii WeatherAPI
                string descriere = json["current"]["condition"]["text"].ToString();
                string temperatura = json["current"]["temp_c"].ToString();
                string image = json["current"]["condition"]["icon"].ToString();

                if (image.StartsWith("//"))
                {
                    image = "https:" + image;
                }
                return (descriere, temperatura, image);
            }
        }
    }
}