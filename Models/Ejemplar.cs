using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public class Ejemplar
    {
        [Key]
        [Display(Name = "Código")]
        public int CodigoEjemplar { get; set; }

        [Required(ErrorMessage = "El libro es obligatorio")]
        [Display(Name = "Libro")]
        public string ISBN { get; set; } = "";

        [StringLength(100, ErrorMessage = "La localización no puede superar los 100 caracteres")]
        [Display(Name = "Localización")]
        public string? Localizacion { get; set; }

        [Required(ErrorMessage = "El estado es obligatorio")]
        [StringLength(20)]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Disponible";

        // Propiedades de navegación
        [ForeignKey(nameof(ISBN))]
        public virtual Libro? Libro { get; set; }

        // Para mostrar en el listado (título del libro)
        [NotMapped]
        public string? LibroTitulo { get; set; }

        public override string ToString()
        {
            return $"Ejemplar #{CodigoEjemplar} - {Localizacion ?? "Sin ubicación"}";
        }
    }
}