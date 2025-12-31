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
            string mesajBucuresti = "";
            string mesajTurda = "";
            string mesajCrypto = "";
            string mesajBursa = "";

            if (!LoadConfiguration())
            {
                return; // Oprim tot dacă nu găsim fisierul
            }
            try
            {
                // 1. Obținem datele
                var vremeBucurestiTask = GetWeatherData("Bucharest");
                var vremeTurdaTaskTask = GetWeatherData("Turda");
                var cryptoService = new CryptoService();
                var stockService = new StockService();


                var mesajCryptoTask =  cryptoService.GetTopCryptoMessageAsync();
                var mesajBursaTask =  stockService.GetStockMarketDataAsync();

                await Task.WhenAll(mesajCryptoTask, mesajBursaTask, vremeBucurestiTask, vremeTurdaTaskTask); // Asteptam ambele

                mesajCrypto = mesajCryptoTask.Result;
                mesajBursa = mesajBursaTask.Result;
                var vremeBucuresti = vremeBucurestiTask.Result;
                var vremeTurda = vremeTurdaTaskTask.Result;

                mesajBucuresti = $"🌍 **Vremea in Bucuresti:**\n" +
                               $"🌡️ Temperatura: {vremeBucuresti.Temperatura}°C\n" +
                               $"☁️ Descriere: {vremeBucuresti.Descriere}\n";

                mesajTurda =
                               $"🌍 **Vremea in Turda:**\n" +
                               $"🌡️ Temperatura: {vremeTurda.Temperatura}°C\n" +
                               $"☁️ Descriere: {vremeTurda.Descriere}\n";

                Console.WriteLine(mesajBucuresti);
                Console.WriteLine(mesajTurda);
                Console.WriteLine(mesajCrypto);
                Console.WriteLine(mesajBursa);

                var botClient = new TelegramBotClient(TelegramBotToken);

                List<string> messages = new List<string> {mesajBucuresti, mesajTurda, mesajCrypto, mesajBursa};

                await botClient.SendMessage(
                    chatId: TelegramChatId,
                    text: "--------------------------------------------\n" + String.Format("{0:f}",   DateTime.Now) + "\n"
                );

                foreach (var message in messages)
                {
                    await botClient.SendMessage(
                        chatId: TelegramChatId,
                        parseMode: ParseMode.Html,
                        text: message
                    );
                }

                Console.WriteLine("Mesaj trimis cu succes!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"A aparut o eroare: {ex.Message}");
                // LOGICA DE BACKUP: Scriem într-un fișier text dacă Telegram nu merge
                try 
                {
                    string logFile = "backup_rapoarte.txt";
                    string dataCurenta = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    
                    // Construim un text simplu de salvat
                    string continutBackup = $"\n--- RAPORT {dataCurenta} (Telegram Failed) ---\n" +
                                            "Din cauza unei erori, mesajul nu a plecat. Iata datele:\n" +
                                            "--- CRIPTO ---\n" + 
                                            mesajBucuresti + "\n" +
                                            mesajTurda + "\n" +
                                            mesajBursa + "\n" +
                                            mesajCrypto + "\n" + // Trebuie sa declari variabila mesajCrypto in afara try-ului principal ca sa o vezi aici
                                            "------------------------------------------\n";

                    // AppendAllText creaza fisierul daca nu exista sau adauga la final
                    File.AppendAllText(logFile, continutBackup);
                    Console.WriteLine("Am salvat datele local in backup_rapoarte.txt");
                }
                catch (Exception fileEx)
                {
                    Console.WriteLine("Nici salvarea in fisier nu a mers: " + fileEx.Message);
                }
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