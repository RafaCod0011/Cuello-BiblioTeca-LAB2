using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public interface IRepositorio<T>
    {
        int Alta(T p);
        int Baja(int id);
        int Modificacion(T p);
        IList<T> ObtenerLista(int paginaNro = 1, int tamPagina= 10);
        int ObtenerCantidad();
        T? ObtenerPorId(int id);
    }
}
        