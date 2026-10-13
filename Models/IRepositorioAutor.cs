using System.Collections.Generic;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public interface IRepositorioAutor : IRepositorio<Autor>
    {
        IList<Autor> BuscarPorNombre(string nombre);
    }
}