using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

public class CryptoService
{
    private readonly HttpClient _httpClient;

    public CryptoService()
    {
        _httpClient = new HttpClient();
        // CoinGecko cere un User-Agent, altfel poate da eroare 403
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "TelegramBotApp/1.0");
    }

    public async Task<string> GetTopCryptoMessageAsync()
    {
        try
        {
            // Definim cele 3 URL-uri pentru USD, EUR și RON
            // Top 5 monede, ordonate după Market Cap
            string baseUrl = "https://api.coingecko.com/api/v3/coins/markets?order=market_cap_desc&per_page=5&page=1&sparkline=false";
            //https://api.coingecko.com/api/v3/coins/markets?vs_currency=usd&order=market_cap_desc&per_page=5&page=1&sparkline=false
            var taskUsd = _httpClient.GetFromJsonAsync<List<CoinInfo>>($"{baseUrl}&vs_currency=usd");
            //var taskEur = _httpClient.GetFromJsonAsync<List<CoinInfo>>($"{baseUrl}&vs_currency=eur");
            //var taskRon = _httpClient.GetFromJsonAsync<List<CoinInfo>>($"{baseUrl}&vs_currency=ron");

            // Așteptăm să se termine toate 3 cererile (merg în paralel, durează puțin)
            await Task.WhenAll(taskUsd);

            var coinsUsd = taskUsd.Result;
            //var coinsEur = taskEur.Result;
            //var coinsRon = taskRon.Result;

            if (coinsUsd == null || coinsUsd.Count == 0) return "⚠️ Nu am putut prelua datele crypto.";

            // Construim mesajul
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("💎 <b>Top 5 Crypto Update</b>\n");

            // Iterăm prin lista de USD și căutăm echivalentul în celelalte liste
            foreach (var coin in coinsUsd)
            {
                // Găsim prețul în EUR și RON pentru aceeași monedă
                //var priceEur = coinsEur.FirstOrDefault(c => c.Id == coin.Id)?.CurrentPrice ?? 0;
                //var priceRon = coinsRon.FirstOrDefault(c => c.Id == coin.Id)?.CurrentPrice ?? 0;

                // Determinăm emoji pentru creștere/scădere
                string trendEmoji = coin.PriceChange24h >= 0 ? "🟢" : "🔴";
                
                sb.AppendLine($"<b>{coin.Name} ({coin.Symbol.ToUpper()})</b> {trendEmoji} {coin.PriceChange24h:F2}%");
                sb.AppendLine($"🇺🇸 ${coin.CurrentPrice:N2}"); // N2 pune virgula la mii și 2 zecimale
               // sb.AppendLine($"🇪🇺 €{priceEur:N2}");
               // sb.AppendLine($"🇷🇴 {priceRon:N2} RON");
                sb.AppendLine("------------------");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Eroare Crypto: {ex.Message}");
            Console.WriteLine(ex.ToString());
            return "⚠️ Eroare la preluarea datelor Crypto.";
        }
    }
}