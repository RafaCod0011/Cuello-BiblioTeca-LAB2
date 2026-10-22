using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public class Prestamo
    {
        [Key]
        [Display(Name = "Código")]
        public int IdPrestamo { get; set; }

        [Required(ErrorMessage = "El socio es obligatorio")]
        [Display(Name = "Socio")]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El ejemplar es obligatorio")]
        [Display(Name = "Ejemplar")]
        public int CodigoEjemplar { get; set; }

        [Required]
        [Display(Name = "Fecha de préstamo")]
        [DataType(DataType.Date)]
        public DateTime FechaPrestamo { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Fecha de vencimiento")]
        [DataType(DataType.Date)]
        public DateTime FechaVencimiento { get; set; } = DateTime.Today.AddDays(15);

        [Display(Name = "Fecha de devolución")]
        [DataType(DataType.Date)]
        public DateTime? FechaDevolucion { get; set; }

        [Required]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Activo";

        [Required]
        [Display(Name = "Bibliotecario")]
        public int IdUsuarioBibliotecario { get; set; }

        // Navegación
        [ForeignKey(nameof(IdUsuario))]
        public virtual Usuario? Socio { get; set; }

        [ForeignKey(nameof(CodigoEjemplar))]
        public virtual Ejemplar? Ejemplar { get; set; }

        [ForeignKey(nameof(IdUsuarioBibliotecario))]
        public virtual Usuario? Bibliotecario { get; set; }

        public virtual Multa? Multa { get; set; }

        // Calculadas
        [NotMapped]
        public bool EstaVencido
        {
            get
            {
                return Estado == "Activo" && FechaVencimiento < DateTime.Today;
            }
        }

        [NotMapped]
        public int DiasAtraso
        {
            get
            {
                if (Estado != "Activo") return 0;
                var dias = (DateTime.Today - FechaVencimiento).Days;
                return dias > 0 ? dias : 0;
            }
        }

        public override string ToString()
        {
            return $"Préstamo #{IdPrestamo}";
        }
    }
}