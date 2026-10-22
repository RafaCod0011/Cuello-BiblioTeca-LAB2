using System;
using System.Collections.Generic;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public interface IRepositorioMulta : IRepositorio<Multa>
    {
        IList<Multa> BuscarConFiltros(string? busqueda, bool? pagada, int pagina, int tamano);
        int ContarConFiltros(string? busqueda, bool? pagada);
        IList<Multa> ObtenerPorSocio(int idSocio);

        void RegistrarPago(int idMulta, int idUsuarioBibliotecario);
    }
}