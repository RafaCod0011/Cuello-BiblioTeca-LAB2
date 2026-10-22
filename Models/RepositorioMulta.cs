using System;
using System.Collections.Generic;
using System.Data;
using MySqlConnector;
using Microsoft.Extensions.Configuration;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public class RepositorioMulta : RepositorioBase, IRepositorioMulta
    {
        public RepositorioMulta(IConfiguration configuration) : base(configuration) { }

        public int Alta(Multa m)
        {
            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"INSERT INTO Multa 
                    (IdPrestamo, FechaDeGeneracion, Importe, Motivo, Pagada, FechaPagada, IdUsuarioBibliotecario)
                    VALUES (@idPrestamo, @fechaGen, @importe, @motivo, @pagada, @fechaPagada, @idBibliotecario);
                    SELECT LAST_INSERT_ID();";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@idPrestamo", m.IdPrestamo);
                    command.Parameters.AddWithValue("@fechaGen", m.FechaDeGeneracion);
                    command.Parameters.AddWithValue("@importe", m.Importe);
                    command.Parameters.AddWithValue("@motivo", string.IsNullOrEmpty(m.Motivo) ? (object)DBNull.Value : m.Motivo);
                    command.Parameters.AddWithValue("@pagada", m.Pagada);
                    command.Parameters.AddWithValue("@fechaPagada", (object?)m.FechaPagada ?? DBNull.Value);
                    command.Parameters.AddWithValue("@idBibliotecario", (object?)m.IdUsuarioBibliotecario ?? DBNull.Value);

                    connection.Open();
                    res = Convert.ToInt32(command.ExecuteScalar());
                    m.IdMulta = res;
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
                string sql = "DELETE FROM Multa WHERE IdMulta = @id";
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

        public int Modificacion(Multa m)
        {
            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"UPDATE Multa 
                               SET Motivo = @motivo,
                                   Importe = @importe
                               WHERE IdMulta = @id";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@motivo", string.IsNullOrEmpty(m.Motivo) ? (object)DBNull.Value : m.Motivo);
                    command.Parameters.AddWithValue("@importe", m.Importe);
                    command.Parameters.AddWithValue("@id", m.IdMulta);
                    connection.Open();
                    res = command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            return res;
        }

        public void RegistrarPago(int idMulta, int idUsuarioBibliotecario)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"
                    UPDATE Multa 
                    SET Pagada = TRUE, 
                        FechaPagada = CURDATE(), 
                        IdUsuarioBibliotecario = @idBibliotecario
                    WHERE IdMulta = @id AND Pagada = FALSE";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@idBibliotecario", idUsuarioBibliotecario);
                    command.Parameters.AddWithValue("@id", idMulta);
                    connection.Open();
                    int res = command.ExecuteNonQuery();
                    connection.Close();

                    if (res == 0)
                        throw new Exception("La multa no existe o ya fue pagada.");
                }
            }
        }

        public IList<Multa> ObtenerLista(int pagina = 1, int tamPagina = 10)
        {
            return BuscarConFiltros(null, null, pagina, tamPagina);
        }

        public int ObtenerCantidad()
        {
            return ContarConFiltros(null, null);
        }

        public IList<Multa> BuscarConFiltros(string? busqueda, bool? pagada, int pagina, int tamano)
        {
            var res = new List<Multa>();
            using (var connection = new MySqlConnection(connectionString))
            {
                var (where, parametros) = ConstruirFiltros(busqueda, pagada);

                string sql = $@"
                    SELECT m.IdMulta, m.IdPrestamo, m.FechaDeGeneracion, m.Importe, m.Motivo, 
                           m.Pagada, m.FechaPagada, m.IdUsuarioBibliotecario,
                           u.Nombre AS SocioNombre, u.Apellido AS SocioApellido, u.Dni AS SocioDni,
                           l.Titulo AS LibroTitulo
                    FROM Multa m
                    INNER JOIN Prestamo p ON m.IdPrestamo = p.IdPrestamo
                    INNER JOIN Usuario u ON p.IdUsuario = u.IdUsuario
                    INNER JOIN Ejemplar e ON p.CodigoEjemplar = e.CodigoEjemplar
                    INNER JOIN Libro l ON e.ISBN = l.ISBN
                    WHERE 1=1 {where}
                    ORDER BY m.Pagada ASC, m.FechaDeGeneracion DESC
                    LIMIT {tamano} OFFSET {(pagina - 1) * tamano}";

                using (var command = new MySqlCommand(sql, connection))
                {
                    foreach (var p in parametros)
                        command.Parameters.AddWithValue(p.Key, p.Value);

                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                        res.Add(MapMulta(reader));
                    connection.Close();
                }
            }
            return res;
        }

        public int ContarConFiltros(string? busqueda, bool? pagada)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                var (where, parametros) = ConstruirFiltros(busqueda, pagada);

                string sql = $@"
                    SELECT COUNT(*) 
                    FROM Multa m
                    INNER JOIN Prestamo p ON m.IdPrestamo = p.IdPrestamo
                    INNER JOIN Usuario u ON p.IdUsuario = u.IdUsuario
                    INNER JOIN Ejemplar e ON p.CodigoEjemplar = e.CodigoEjemplar
                    INNER JOIN Libro l ON e.ISBN = l.ISBN
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

        public Multa? ObtenerPorId(int id)
        {
            Multa? m = null;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"
                    SELECT m.IdMulta, m.IdPrestamo, m.FechaDeGeneracion, m.Importe, m.Motivo, 
                           m.Pagada, m.FechaPagada, m.IdUsuarioBibliotecario,
                           u.Nombre AS SocioNombre, u.Apellido AS SocioApellido, u.Dni AS SocioDni,
                           l.Titulo AS LibroTitulo
                    FROM Multa m
                    INNER JOIN Prestamo p ON m.IdPrestamo = p.IdPrestamo
                    INNER JOIN Usuario u ON p.IdUsuario = u.IdUsuario
                    INNER JOIN Ejemplar e ON p.CodigoEjemplar = e.CodigoEjemplar
                    INNER JOIN Libro l ON e.ISBN = l.ISBN
                    WHERE m.IdMulta = @id";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                        m = MapMulta(reader);
                    connection.Close();
                }
            }
            return m;
        }

        public IList<Multa> ObtenerPorSocio(int idSocio)
        {
            var res = new List<Multa>();
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"
                    SELECT m.IdMulta, m.IdPrestamo, m.FechaDeGeneracion, m.Importe, m.Motivo, 
                           m.Pagada, m.FechaPagada, m.IdUsuarioBibliotecario,
                           u.Nombre AS SocioNombre, u.Apellido AS SocioApellido, u.Dni AS SocioDni,
                           l.Titulo AS LibroTitulo
                    FROM Multa m
                    INNER JOIN Prestamo p ON m.IdPrestamo = p.IdPrestamo
                    INNER JOIN Usuario u ON p.IdUsuario = u.IdUsuario
                    INNER JOIN Ejemplar e ON p.CodigoEjemplar = e.CodigoEjemplar
                    INNER JOIN Libro l ON e.ISBN = l.ISBN
                    WHERE p.IdUsuario = @idSocio
                    ORDER BY m.Pagada ASC, m.FechaDeGeneracion DESC";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@idSocio", idSocio);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                        res.Add(MapMulta(reader));
                    connection.Close();
                }
            }
            return res;
        }

        private (string where, Dictionary<string, object> parametros) ConstruirFiltros(
            string? busqueda, bool? pagada)
        {
            var where = "";
            var parametros = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                where += @" AND (u.Nombre LIKE @busqueda 
                            OR u.Apellido LIKE @busqueda 
                            OR u.Dni LIKE @busqueda 
                            OR l.Titulo LIKE @busqueda)";
                parametros["@busqueda"] = "%" + busqueda + "%";
            }

            if (pagada.HasValue)
            {
                where += " AND m.Pagada = @pagada";
                parametros["@pagada"] = pagada.Value;
            }

            return (where, parametros);
        }

        private Multa MapMulta(MySqlDataReader reader)
        {
            var m = new Multa
            {
                IdMulta = reader.GetInt32("IdMulta"),
                IdPrestamo = reader.GetInt32("IdPrestamo"),
                FechaDeGeneracion = reader.GetDateTime("FechaDeGeneracion"),
                Importe = reader.GetDecimal("Importe"),
                Motivo = reader.IsDBNull(reader.GetOrdinal("Motivo")) ? null : reader.GetString("Motivo"),
                Pagada = reader.GetBoolean("Pagada"),
                FechaPagada = reader.IsDBNull(reader.GetOrdinal("FechaPagada"))
                    ? null : reader.GetDateTime("FechaPagada"),
                IdUsuarioBibliotecario = reader.IsDBNull(reader.GetOrdinal("IdUsuarioBibliotecario"))
                    ? null : reader.GetInt32("IdUsuarioBibliotecario")
            };

            if (HasColumn(reader, "SocioNombre"))
            {
                m.SocioNombre = $"{reader.GetString("SocioNombre")} {reader.GetString("SocioApellido")}";
                m.SocioDni = reader.IsDBNull(reader.GetOrdinal("SocioDni"))
                    ? "" : reader.GetString("SocioDni");
            }

            if (HasColumn(reader, "LibroTitulo") && !reader.IsDBNull(reader.GetOrdinal("LibroTitulo")))
                m.LibroTitulo = reader.GetString("LibroTitulo");

            return m;
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