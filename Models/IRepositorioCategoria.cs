using System.Collections.Generic;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public interface IRepositorioCategoria : IRepositorio<Categoria>
    {
        IList<Categoria> BuscarPorNombre(string nombre);
        Categoria? ObtenerPorNombre(string nombre);
    }
}