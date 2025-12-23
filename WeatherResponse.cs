using System.Text.Json.Serialization; // Asigura-te ca ai acest using sus de tot

// Record-ul principal care "tine" tot raspunsul
public record WeatherResponse(
    [property: JsonPropertyName("location")] Location Location,
    [property: JsonPropertyName("current")] Current Current
);

public record Location(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("region")] string Region,
    [property: JsonPropertyName("country")] string Country
);

public record Current(
    [property: JsonPropertyName("temp_c")] double TempC,
    [property: JsonPropertyName("condition")] Condition Condition
);

public record Condition(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("icon")] string Icon
);