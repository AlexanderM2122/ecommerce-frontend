using System.ComponentModel.DataAnnotations;

namespace EcommerceFrontend.Models;

public class CategoriaForm
{
    public const int NombreMinLength = 2;
    public const int NombreMaxLength = 100;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(NombreMaxLength, MinimumLength = NombreMinLength,
        ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}