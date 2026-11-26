using System.ComponentModel.DataAnnotations;

namespace EcommerceFrontend.Models;

public class RegistroForm
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(UsuarioCrearForm.NombreMaxLength, MinimumLength = UsuarioCrearForm.NombreMinLength,
        ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [StringLength(UsuarioCrearForm.EmailMaxLength, ErrorMessage = "El correo no debe exceder 254 caracteres.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(UsuarioCrearForm.PasswordMaxLength, MinimumLength = UsuarioCrearForm.PasswordMinLength,
        ErrorMessage = "La contraseña debe tener entre 8 y 72 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,72}$",
        ErrorMessage = "La contraseña debe incluir mayúscula, minúscula, número y carácter especial.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma tu contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    public string ConfirmarPassword { get; set; } = string.Empty;
}