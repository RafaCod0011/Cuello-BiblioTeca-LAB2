using System;
using System.Collections.Generic;
using System.Data;
using MySqlConnector;
using Microsoft.Extensions.Configuration;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public class RepositorioReserva : RepositorioBase, IRepositorioReserva
    {
        public RepositorioReserva(IConfiguration configuration) : base(configuration) { }

        public int Alta(Reserva r)
        {
            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"INSERT INTO Reserva 
                    (IdUsuario, ISBN, FechaReserva, FechaVencimiento, Estado)
                    VALUES (@idUsuario, @isbn, @fechaReserva, @fechaVencimiento, @estado);
                    SELECT LAST_INSERT_ID();";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@idUsuario", r.IdUsuario);
                    command.Parameters.AddWithValue("@isbn", r.ISBN);
                    command.Parameters.AddWithValue("@fechaReserva", r.FechaReserva);
                    command.Parameters.AddWithValue("@fechaVencimiento", (object?)r.FechaVencimiento ?? DBNull.Value);
                    command.Parameters.AddWithValue("@estado", r.Estado);

                    connection.Open();
                    res = Convert.ToInt32(command.ExecuteScalar());
                    r.IdReserva = res;
                    connection.Close();
                }
            }
            return res;
        }

        public int Baja(int id)
        {
            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = "DELETE FROM Reserva WHERE IdReserva = @id";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    connection.Open();
                    res = command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            return res;
        }

        public int Modificacion(Reserva r)
        {
            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"UPDATE Reserva 
                               SET Estado = @estado,
                                   FechaVencimiento = @fechaVencimiento
                               WHERE IdReserva = @id";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@estado", r.Estado);
                    command.Parameters.AddWithValue("@fechaVencimiento", (object?)r.FechaVencimiento ?? DBNull.Value);
                    command.Parameters.AddWithValue("@id", r.IdReserva);
                    connection.Open();
                    res = command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            return res;
        }

        public void Cancelar(int idReserva)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"UPDATE Reserva 
                               SET Estado = 'Cancelada' 
                               WHERE IdReserva = @id AND Estado = 'Pendiente'";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", idReserva);
                    connection.Open();
                    int res = command.ExecuteNonQuery();
                    connection.Close();

                    if (res == 0)
                        throw new Exception("La reserva no existe o ya fue cancelada.");
                }
            }
        }

        public IList<Reserva> ObtenerLista(int pagina = 1, int tamPagina = 10)
        {
            return BuscarConFiltros(null, null, null, pagina, tamPagina);
        }

        public int ObtenerCantidad()
        {
            return ContarConFiltros(null, null, null);
        }

        public IList<Reserva> BuscarConFiltros(string? busqueda, string? estado, int? idSocio, int pagina, int tamano)
        {
            var res = new List<Reserva>();
            using (var connection = new MySqlConnection(connectionString))
            {
                var (where, parametros) = ConstruirFiltros(busqueda, estado, idSocio);

                string sql = $@"
                    SELECT r.IdReserva, r.IdUsuario, r.ISBN, r.FechaReserva, 
                           r.FechaVencimiento, r.Estado,
                           u.Nombre AS SocioNombre, u.Apellido AS SocioApellido, u.Dni AS SocioDni,
                           l.Titulo AS LibroTitulo
                    FROM Reserva r
                    INNER JOIN Usuario u ON r.IdUsuario = u.IdUsuario
                    INNER JOIN Libro l ON r.ISBN = l.ISBN
                    WHERE 1=1 {where}
                    ORDER BY 
                        CASE WHEN r.Estado = 'Pendiente' THEN 0 ELSE 1 END,
                        r.FechaReserva DESC
                    LIMIT {tamano} OFFSET {(pagina - 1) * tamano}";

                using (var command = new MySqlCommand(sql, connection))
                {
                    foreach (var p in parametros)
                        command.Parameters.AddWithValue(p.Key, p.Value);

                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                        res.Add(MapReserva(reader));
                    connection.Close();
                }
            }
            return res;
        }

        public int ContarConFiltros(string? busqueda, string? estado, int? idSocio)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                var (where, parametros) = ConstruirFiltros(busqueda, estado, idSocio);

                string sql = $@"
                    SELECT COUNT(*) 
                    FROM Reserva r
                    INNER JOIN Usuario u ON r.IdUsuario = u.IdUsuario
                    INNER JOIN Libro l ON r.ISBN = l.ISBN
                    WHERE 1=1 {where}";

                using (var command = new MySqlCommand(sql, connection))
                {
                    foreach (var p in parametros)
                        command.Parameters.AddWithValue(p.Key, p.Value);

                    connection.Open();
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public IList<Reserva> ObtenerPorSocio(int idSocio, int pagina, int tamano)
        {
            return BuscarConFiltros(null, null, idSocio, pagina, tamano);
        }

        public int ContarPorSocio(int idSocio)
        {
            return ContarConFiltros(null, null, idSocio);
        }

        public Reserva? ObtenerPorId(int id)
        {
            Reserva? r = null;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"
                    SELECT r.IdReserva, r.IdUsuario, r.ISBN, r.FechaReserva, 
                           r.FechaVencimiento, r.Estado,
                           u.Nombre AS SocioNombre, u.Apellido AS SocioApellido, u.Dni AS SocioDni,
                           l.Titulo AS LibroTitulo
                    FROM Reserva r
                    INNER JOIN Usuario u ON r.IdUsuario = u.IdUsuario
                    INNER JOIN Libro l ON r.ISBN = l.ISBN
                    WHERE r.IdReserva = @id";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                        r = MapReserva(reader);
                    connection.Close();
                }
            }
            return r;
        }

        public bool TieneReservaPendiente(int idSocio, string isbn)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"
                    SELECT COUNT(*) FROM Reserva 
                    WHERE IdUsuario = @idSocio 
                      AND ISBN = @isbn 
                      AND Estado = 'Pendiente'";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@idSocio", idSocio);
                    command.Parameters.AddWithValue("@isbn", isbn);
                    connection.Open();
                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }
        }
        public int ContarReservasPendientesPorSocio(int idSocio)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = "SELECT COUNT(*) FROM Reserva WHERE IdUsuario = @id AND Estado = 'Pendiente'";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", idSocio);
                    connection.Open();
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        private (string where, Dictionary<string, object> parametros) ConstruirFiltros(
            string? busqueda, string? estado, int? idSocio)
        {
            var where = "";
            var parametros = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                where += @" AND (u.Nombre LIKE @busqueda 
                            OR u.Apellido LIKE @busqueda 
                            OR u.Dni LIKE @busqueda 
                            OR l.Titulo LIKE @busqueda 
                            OR l.ISBN LIKE @busqueda)";
                parametros["@busqueda"] = "%" + busqueda + "%";
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                where += " AND r.Estado = @estado";
                parametros["@estado"] = estado;
            }

            if (idSocio.HasValue)
            {
                where += " AND r.IdUsuario = @idSocio";
                parametros["@idSocio"] = idSocio.Value;
            }

            return (where, parametros);
        }

        private Reserva MapReserva(MySqlDataReader reader)
        {
            var r = new Reserva
            {
                IdReserva = reader.GetInt32("IdReserva"),
                IdUsuario = reader.GetInt32("IdUsuario"),
                ISBN = reader.GetString("ISBN"),
                FechaReserva = reader.GetDateTime("FechaReserva"),
                FechaVencimiento = reader.IsDBNull(reader.GetOrdinal("FechaVencimiento"))
                    ? null : reader.GetDateTime("FechaVencimiento"),
                Estado = reader.GetString("Estado")
            };

            if (HasColumn(reader, "SocioNombre"))
            {
                r.SocioNombre = $"{reader.GetString("SocioNombre")} {reader.GetString("SocioApellido")}";
                r.SocioDni = reader.IsDBNull(reader.GetOrdinal("SocioDni"))
                    ? "" : reader.GetString("SocioDni");
            }

            if (HasColumn(reader, "LibroTitulo") && !reader.IsDBNull(reader.GetOrdinal("LibroTitulo")))
                r.LibroTitulo = reader.GetString("LibroTitulo");

            return r;
        }

        private bool HasColumn(MySqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}