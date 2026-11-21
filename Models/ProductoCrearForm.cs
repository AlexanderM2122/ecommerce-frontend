
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EcommerceFrontend.Models;

public class ProductoCrearForm
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(255)]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    public string Descripcion { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "99999999.99",
        ErrorMessage = "El precio debe ser mayor que cero.")]
    public decimal Precio { get; set; }

    public List<int> CategoriaIds { get; set; } = [];

    [ValidateNever]
    public List<Categoria> CategoriasDisponibles { get; set; } = [];
}
