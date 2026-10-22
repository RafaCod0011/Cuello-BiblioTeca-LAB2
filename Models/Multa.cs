using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public class Multa
    {
        [Key]
        [Display(Name = "Código")]
        public int IdMulta { get; set; }

        [Required]
        [Display(Name = "Préstamo")]
        public int IdPrestamo { get; set; }

        [Required]
        [Display(Name = "Fecha de generación")]
        public DateTime FechaDeGeneracion { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "El importe es obligatorio")]
        [Range(0.01, 999999.99, ErrorMessage = "El importe debe ser mayor a 0")]
        [Display(Name = "Importe")]
        [DataType(DataType.Currency)]
        public decimal Importe { get; set; }

        [StringLength(200)]
        [Display(Name = "Motivo")]
        public string? Motivo { get; set; }

        [Required]
        [Display(Name = "Pagada")]
        public bool Pagada { get; set; } = false;

        [Display(Name = "Fecha de pago")]
        [DataType(DataType.Date)]
        public DateTime? FechaPagada { get; set; }

        [Display(Name = "Cobrada por")]
        public int? IdUsuarioBibliotecario { get; set; }

        // Navegación
        [ForeignKey(nameof(IdPrestamo))]
        public virtual Prestamo? Prestamo { get; set; }

        [ForeignKey(nameof(IdUsuarioBibliotecario))]
        public virtual Usuario? Bibliotecario { get; set; }

        // Propiedades para el listado 
        [NotMapped]
        public string? SocioNombre { get; set; }

        [NotMapped]
        public string? SocioDni { get; set; }

        [NotMapped]
        public string? LibroTitulo { get; set; }

        public override string ToString()
        {
            return $"Multa #{IdMulta} - ${Importe:N2}";
        }
    }
}