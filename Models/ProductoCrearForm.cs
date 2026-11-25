using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EcommerceFrontend.Models;

public class ProductoCrearForm
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(255, MinimumLength = 2,
        ErrorMessage = "El título debe tener entre 2 y 255 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(2000, MinimumLength = 5,
        ErrorMessage = "La descripción debe tener entre 5 y 2000 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(typeof(decimal), "0.01", "99999999.99",
        ErrorMessage = "El precio debe estar entre 0.01 y 99,999,999.99.")]
    [RegularExpression(@"^\d+(\.\d{1,2})?$",
        ErrorMessage = "El precio debe ser un número válido con máximo 2 decimales.")]
    public decimal Precio { get; set; }

    public List<int> CategoriaIds { get; set; } = [];

    [ValidateNever]
    public List<Categoria> CategoriasDisponibles { get; set; } = [];
}