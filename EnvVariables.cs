using System.Text.Json.Serialization; // Asigura-te ca ai acest using sus de tot

public record EnvVariables(
    [property: JsonPropertyName("WeatherApiKey")] string WeatherApiKey,
    [property: JsonPropertyName("TelegramBotToken")] string TelegramBotToken,
    [property: JsonPropertyName("TelegramChatId")] long TelegramChatId
);