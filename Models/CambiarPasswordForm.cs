using System.ComponentModel.DataAnnotations;

namespace EcommerceFrontend.Models;

public sealed class CambiarPasswordForm
{
    [Required(ErrorMessage = "Ingresa tu contrase�a actual.")]
    [DataType(DataType.Password)]
    public string PasswordActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa una contrase�a nueva.")]
    [StringLength(UsuarioCrearForm.PasswordMaxLength, MinimumLength = UsuarioCrearForm.PasswordMinLength,
        ErrorMessage = "Usa entre 8 y 72 caracteres.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma la contrase�a nueva.")]
    [Compare(nameof(Password), ErrorMessage = "Las contrase�as deben coincidir.")]
    [DataType(DataType.Password)]
    public string ConfirmarPassword { get; set; } = string.Empty;
}
