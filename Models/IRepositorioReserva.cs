using System;
using System.Collections.Generic;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public interface IRepositorioReserva : IRepositorio<Reserva>
    {
        IList<Reserva> BuscarConFiltros(string? busqueda, string? estado, int? idSocio, int pagina, int tamano);
        int ContarConFiltros(string? busqueda, string? estado, int? idSocio);
        IList<Reserva> ObtenerPorSocio(int idSocio, int pagina, int tamano);
        int ContarPorSocio(int idSocio);

        bool TieneReservaPendiente(int idSocio, string isbn);

        int ContarReservasPendientesPorSocio(int idSocio);

        // Operaciones
        void Cancelar(int idReserva);
    }
}