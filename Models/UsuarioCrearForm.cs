
using System.ComponentModel.DataAnnotations;

namespace EcommerceFrontend.Models;

public class UsuarioCrearForm
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(255)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(72, MinimumLength = 8,
        ErrorMessage = "La contraseña debe tener entre 8 y 72 caracteres.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona un rol.")]
    [RegularExpression("^(Administrador|Usuario)$",
        ErrorMessage = "Selecciona un rol válido.")]
    public string Rol { get; set; } = string.Empty;
}
