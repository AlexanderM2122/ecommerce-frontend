
using System.ComponentModel.DataAnnotations;

namespace EcommerceFrontend.Models;

public class ProductoEditarForm
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(255, ErrorMessage = "Máximo 255 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    public string Descripcion { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "99999999.99",
        ErrorMessage = "El precio debe ser mayor que cero.")]
    public decimal Precio { get; set; }
}
