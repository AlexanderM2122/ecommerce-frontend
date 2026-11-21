
using System.Text.Json.Serialization;

namespace EcommerceFrontend.Models;

public class Categoria
{
    [JsonPropertyName("categoriaid")]
    public int CategoriaId { get; set; }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("protegida")]
    public bool Protegida { get; set; }
}
