
using System.Text.Json.Serialization;

namespace EcommerceFrontend.Models;

public class Usuario
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("rol")]
    public string Rol { get; set; } = string.Empty;
}
