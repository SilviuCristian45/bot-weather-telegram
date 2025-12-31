using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using YahooFinanceApi; // <--- Nu uita using-ul asta dupa instalare

public class StockService
{
    public async Task<string> GetStockMarketDataAsync()
    {
        try
        {
            // Lista de simboluri pe care le urmarim
            var symbols = new string[] { "^GSPC", "AAPL", "TSLA", "MSFT", "EURUSD=X", "RON=X" };

            // Cerem datele de la Yahoo
            // Field.RegularMarketPrice = Pretul curent
            // Field.RegularMarketChangePercent = Cat a crescut/scazut azi (%)
            var securities = await Yahoo.Symbols(symbols)
                                        .Fields(Field.RegularMarketPrice, Field.RegularMarketChangePercent, Field.ShortName)
                                        .QueryAsync();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("📈 <b>Bursa & Valute (Yahoo)</b>\n");

            // 1. S&P 500 (Piata Generala)
            if (securities.ContainsKey("^GSPC"))
            {
                var sp500 = securities["^GSPC"];
                string emoji = sp500.RegularMarketChangePercent >= 0 ? "🟢" : "🔴";
                sb.AppendLine($"🇺🇸 <b>S&P 500</b>: {sp500.RegularMarketPrice:N0} pts ({emoji} {sp500.RegularMarketChangePercent:F2}%)");
            }

            sb.AppendLine("------------------");

            // 2. Actiuni Individuale (Tech)
            // Facem o lista mica locala pentru iterare usoara
            var stocks = new[] { "AAPL", "TSLA", "MSFT" };
            foreach (var ticker in stocks)
            {
                if (securities.ContainsKey(ticker))
                {
                    var stock = securities[ticker];
                    string emoji = stock.RegularMarketChangePercent >= 0 ? "🟢" : "🔴";
                    sb.AppendLine($"🏢 <b>{stock.ShortName}</b>: ${stock.RegularMarketPrice:N2} ({emoji} {stock.RegularMarketChangePercent:F2}%)");
                }
            }

            sb.AppendLine("------------------");

            // 3. Valute (Informational)
            if (securities.ContainsKey("EURUSD=X") && securities.ContainsKey("RON=X"))
            {
                var eurUsd = securities["EURUSD=X"].RegularMarketPrice;
                var usdRon = securities["RON=X"].RegularMarketPrice;
                var eurRon = eurUsd * usdRon; // Calculam noi Euro in Lei

                sb.AppendLine("💱 <b>Curs Valutar (Live):</b>");
                sb.AppendLine($"💵 1 USD = {usdRon:F4} RON");
                sb.AppendLine($"💶 1 EUR = {eurRon:F4} RON");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Eroare StockService: {ex.Message}");
            return "⚠️ Datele despre bursa nu sunt disponibile momentan.";
        }
    }
}