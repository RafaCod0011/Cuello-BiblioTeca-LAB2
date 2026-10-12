using System.ComponentModel.DataAnnotations;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public class LoginView
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [Display(Name = "Email")]
        public string Usuario { get; set; } = "";

        [Required(ErrorMessage = "La clave es obligatoria")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Clave { get; set; } = "";
    }
}