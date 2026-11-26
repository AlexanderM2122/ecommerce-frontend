using System.ComponentModel.DataAnnotations;

namespace EcommerceFrontend.Models;

public class UsuarioCrearForm
{
    public const int EmailMaxLength = 254;
    public const int NombreMinLength = 2;
    public const int NombreMaxLength = 100;
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 72;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [StringLength(EmailMaxLength, ErrorMessage = "El correo no debe exceder 254 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(NombreMaxLength, MinimumLength = NombreMinLength,
        ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(PasswordMaxLength, MinimumLength = PasswordMinLength,
        ErrorMessage = "La contraseña debe tener entre 8 y 72 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,72}$",
        ErrorMessage = "La contraseña debe incluir mayúscula, minúscula, número y carácter especial.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona un rol.")]
    [RegularExpression("^(Administrador|Usuario)$",
        ErrorMessage = "Selecciona un rol válido.")]
    public string Rol { get; set; } = string.Empty;
}