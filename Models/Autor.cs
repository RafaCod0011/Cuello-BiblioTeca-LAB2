using System.ComponentModel.DataAnnotations;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public class Autor
    {
        [Key]
        [Display(Name = "Código")]
        public int IdAutor { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres")]
        public string Apellido { get; set; } = "";

        [StringLength(100, ErrorMessage = "La nacionalidad no puede superar los 100 caracteres")]
        public string? Nacionalidad { get; set; }

        public override string ToString()
        {
            var res = $"{Nombre} {Apellido}";
            if (!string.IsNullOrEmpty(Nacionalidad))
                res += $" ({Nacionalidad})";
            return res;
        }
    }
}