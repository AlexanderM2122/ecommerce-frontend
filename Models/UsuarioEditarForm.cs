
using System.ComponentModel.DataAnnotations;

namespace EcommerceFrontend.Models;

public class UsuarioEditarForm
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(255)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona un rol.")]
    [RegularExpression("^(Administrador|Usuario)$",
        ErrorMessage = "Selecciona un rol válido.")]
    public string Rol { get; set; } = string.Empty;
}
