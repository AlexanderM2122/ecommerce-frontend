
using System.ComponentModel.DataAnnotations;

namespace EcommerceFrontend.Models;

public class CategoriaForm
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(255, ErrorMessage = "Máximo 255 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}
