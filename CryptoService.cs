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
            string baseUrl = "https://api.coingecko.com/api/v3/coins/markets?order=market_cap_desc&per_page=5&page=1&sparkline=false";
            var taskUsd = await _httpClient.GetFromJsonAsync<List<CoinInfo>>($"{baseUrl}&vs_currency=usd");
          
            var coinsUsd = taskUsd;

            if (coinsUsd == null || coinsUsd.Count == 0) return "⚠️ Nu am putut prelua datele crypto.";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("💎 Top 5 Crypto Update\n");

            foreach (var coin in coinsUsd)
            {
                string trendEmoji = coin.PriceChange24h >= 0 ? "🟢" : "🔴";
                
                sb.AppendLine($"{coin.Name} ({coin.Symbol.ToUpper()}) {trendEmoji} {coin.PriceChange24h:F2}%");
                sb.AppendLine($"🇺🇸 ${coin.CurrentPrice:N2}");
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