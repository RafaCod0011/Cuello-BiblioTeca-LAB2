using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public class Reserva
    {
        [Key]
        [Display(Name = "Código")]
        public int IdReserva { get; set; }

        [Required(ErrorMessage = "El socio es obligatorio")]
        [Display(Name = "Socio")]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El libro es obligatorio")]
        [Display(Name = "Libro")]
        public string ISBN { get; set; } = "";

        [Required]
        [Display(Name = "Fecha de reserva")]
        public DateTime FechaReserva { get; set; } = DateTime.Now;

        [Display(Name = "Fecha de expiración")]
        public DateTime? FechaVencimiento { get; set; } = DateTime.Now.AddHours(48);

        [Required]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Pendiente";

        // Navegación
        [ForeignKey(nameof(IdUsuario))]
        public virtual Usuario? Socio { get; set; }

        [ForeignKey(nameof(ISBN))]
        public virtual Libro? Libro { get; set; }

        // Propiedades para el listado
        [NotMapped]
        public string? SocioNombre { get; set; }

        [NotMapped]
        public string? SocioDni { get; set; }

        [NotMapped]
        public string? LibroTitulo { get; set; }

        [NotMapped]
        public bool EstaExpirada
        {
            get
            {
                return Estado == "Pendiente" 
                    && FechaVencimiento.HasValue 
                    && FechaVencimiento.Value < DateTime.Now;
            }
        }

        public override string ToString()
        {
            return $"Reserva #{IdReserva}";
        }
    }
}