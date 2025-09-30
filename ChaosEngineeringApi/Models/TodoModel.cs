using System.Text.Json.Serialization;

namespace ChaosEngineeringApi.Models;

public record TodoModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("completed")]
    public bool Completed { get; set; }

}
