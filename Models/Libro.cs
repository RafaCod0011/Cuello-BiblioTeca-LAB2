using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public class Libro
    {
        [Key]
        [Display(Name = "ISBN")]
        [Required(ErrorMessage = "El ISBN es obligatorio")]
        [StringLength(20, ErrorMessage = "El ISBN no puede superar los 20 caracteres")]
        [RegularExpression(@"^(?:\d{9}[\dXx]|\d{13})$", ErrorMessage = "El ISBN debe tener 10 o 13 dígitos")]
        public string ISBN { get; set; } = "";

        [Required(ErrorMessage = "El título es obligatorio")]
        [StringLength(300, ErrorMessage = "El título no puede superar los 300 caracteres")]
        public string Titulo { get; set; } = "";

        [Display(Name = "Nro. de Páginas")]
        [Range(1, 10000, ErrorMessage = "El número de páginas debe estar entre 1 y 10000")]
        public int? NroPaginas { get; set; }

        [StringLength(150, ErrorMessage = "La editorial no puede superar los 150 caracteres")]
        public string? Editorial { get; set; }

        [Display(Name = "Año de Publicación")]
        [Range(1, 2100, ErrorMessage = "El año debe estar entre 1 y 2100")]
        public int? AnioPublicacion { get; set; }

        public string? Sinopsis { get; set; }

        [Display(Name = "Portada")]
        public string? Portada { get; set; }

        [NotMapped]
        [Display(Name = "Imagen de portada")]
        public IFormFile? PortadaFile { get; set; }

        [Required(ErrorMessage = "La categoría es obligatoria")]
        [Display(Name = "Categoría")]
        public int? IdCategoria { get; set; }

        // Propiedades de navegación 

        [ForeignKey(nameof(IdCategoria))]
        public virtual Categoria? Categoria { get; set; }

        [NotMapped]
        public List<int> IdAutores { get; set; } = new List<int>();

        [NotMapped]
        public List<Autor> Autores { get; set; } = new List<Autor>();

        //  Propiedades calculadas para usar con Ajax.

        [NotMapped]
        public string? AutoresTexto { get; set; }

        [NotMapped]
        public string? CategoriaNombre { get; set; }

        [NotMapped]
        public int TotalEjemplares { get; set; }

        [NotMapped]
        public int EjemplaresDisponibles { get; set; }

        public override string ToString() => Titulo;
    }
}