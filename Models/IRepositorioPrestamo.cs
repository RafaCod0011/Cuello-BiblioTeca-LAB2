using System;
using System.Collections.Generic;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public interface IRepositorioPrestamo : IRepositorio<Prestamo>
    {
        IList<Prestamo> BuscarConFiltros(string? busqueda, string? estado, bool soloVencidos, int pagina, int tamano);
        int ContarConFiltros(string? busqueda, string? estado, bool soloVencidos);
        IList<Prestamo> ObtenerPorSocio(int idSocio, int pagina, int tamano);
        int ContarPorSocio(int idSocio);

        int ContarPrestamosActivosPorSocio(int idSocio);
        int ContarMultasPendientesPorSocio(int idSocio);
        int ContarPrestamosVencidosPorSocio(int idSocio);
        bool TienePrestamoActivoDelLibro(int idSocio, string isbn);
        bool TienePrestamoActivoDelEjemplar(int codigoEjemplar);

        void RegistrarDevolucion(int idPrestamo, DateTime fechaDevolucion);
    }
}