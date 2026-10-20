using System.Collections.Generic;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public interface IRepositorioLibro
    {
        // CRUD
        int Alta(Libro l);
        int Modificacion(Libro l);
        int Baja(string isbn);
        Libro? ObtenerPorIsbn(string isbn);

        // Listado paginado
        IList<Libro> ObtenerLista(int pagina = 1, int tamPagina = 6);
        int ObtenerCantidad();

        // Validaciones
        bool ExisteIsbn(string isbn, string? isbnExcluir = null);

        // Relaciones
        IList<Autor> ObtenerAutores(string isbn);

        // Contadores
        int ObtenerCantidadEjemplares(string isbn);
        int ObtenerCantidadEjemplaresDisponibles(string isbn);

        IList<Libro> BuscarPorTituloOIsbn(string term);

        IList<Libro> ObtenerPorAutor(int idAutor, int pagina, int tamano);
        int ContarPorAutor(int idAutor);
    }
}