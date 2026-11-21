
using System.Text.Json.Serialization;

namespace EcommerceFrontend.Models;

public class Archivo
{
    [JsonPropertyName("archivoId")]
    public int ArchivoId { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("mime")]
    public string Mime { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("indb")]
    public bool InDb { get; set; }
}
