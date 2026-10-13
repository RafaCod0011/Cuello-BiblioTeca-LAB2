using System.ComponentModel.DataAnnotations;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public class Categoria
    {
        [Key]
        [Display(Name = "Código")]
        public int IdCategoria { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres")]
        public string Nombre { get; set; } = "";

        [StringLength(300, ErrorMessage = "La descripción no puede superar los 300 caracteres")]
        public string? Descripcion { get; set; }

        public override string ToString()
        {
            return Nombre;
        }
    }
}