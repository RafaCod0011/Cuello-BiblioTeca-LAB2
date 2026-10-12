using System.ComponentModel.DataAnnotations;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public class CambioClaveView
    {
        public int Id { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string ClaveVieja { get; set; } = "";

        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [StringLength(50, ErrorMessage = "La clave debe tener entre 3 y 50 caracteres", MinimumLength = 3)]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string ClaveNueva { get; set; } = "";

        [Required(ErrorMessage = "Debe repetir la contraseña nueva")]
        [StringLength(50, ErrorMessage = "La clave debe tener entre 3 y 50 caracteres", MinimumLength = 3)]
        [DataType(DataType.Password)]
        [Compare("ClaveNueva", ErrorMessage = "Las contraseñas no coinciden")]
        [Display(Name = "Repetir contraseña")]
        public string ClaveRepeticion { get; set; } = "";
    }
}