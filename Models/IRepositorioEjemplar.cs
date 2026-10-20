using System.Collections.Generic;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public interface IRepositorioEjemplar : IRepositorio<Ejemplar>
    {
        IList<Ejemplar> BuscarConFiltros(string? busqueda, string? estado, int pagina, int tamano);
        int ContarConFiltros(string? busqueda, string? estado);
        IList<Ejemplar> ObtenerPorLibro(string isbn);
        bool TienePrestamosActivos(int codigoEjemplar);
    }
}