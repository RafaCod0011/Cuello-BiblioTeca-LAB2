using System;
using System.Collections.Generic;
using System.Data;
using MySqlConnector;
using Microsoft.Extensions.Configuration;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public class RepositorioEjemplar : RepositorioBase, IRepositorioEjemplar
    {
        public RepositorioEjemplar(IConfiguration configuration) : base(configuration) { }

        public int Alta(Ejemplar e)
        {
            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"INSERT INTO Ejemplar (ISBN, Localizacion, Estado) 
                               VALUES (@isbn, @localizacion, @estado);
                               SELECT LAST_INSERT_ID();";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@isbn", e.ISBN);
                    command.Parameters.AddWithValue("@localizacion",
                        string.IsNullOrEmpty(e.Localizacion) ? (object)DBNull.Value : e.Localizacion);
                    command.Parameters.AddWithValue("@estado", e.Estado);
                    connection.Open();
                    res = Convert.ToInt32(command.ExecuteScalar());
                    e.CodigoEjemplar = res;
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
                string sql = "DELETE FROM Ejemplar WHERE CodigoEjemplar = @id";
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

        public int Modificacion(Ejemplar e)
        {
            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"UPDATE Ejemplar 
                               SET ISBN = @isbn, Localizacion = @localizacion, Estado = @estado
                               WHERE CodigoEjemplar = @id";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@isbn", e.ISBN);
                    command.Parameters.AddWithValue("@localizacion",
                        string.IsNullOrEmpty(e.Localizacion) ? (object)DBNull.Value : e.Localizacion);
                    command.Parameters.AddWithValue("@estado", e.Estado);
                    command.Parameters.AddWithValue("@id", e.CodigoEjemplar);
                    connection.Open();
                    res = command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            return res;
        }

        public IList<Ejemplar> ObtenerLista(int pagina = 1, int tamPagina = 10)
        {
            return BuscarConFiltros(null, null, pagina, tamPagina);
        }

        public int ObtenerCantidad()
        {
            return ContarConFiltros(null, null);
        }

        public IList<Ejemplar> BuscarConFiltros(string? busqueda, string? estado, int pagina, int tamano)
        {
            var res = new List<Ejemplar>();
            using (var connection = new MySqlConnection(connectionString))
            {
                var (where, parametros) = ConstruirFiltros(busqueda, estado);

                string sql = $@"
                    SELECT e.CodigoEjemplar, e.ISBN, e.Localizacion, e.Estado,
                           l.Titulo AS LibroTitulo
                    FROM Ejemplar e
                    INNER JOIN Libro l ON e.ISBN = l.ISBN
                    WHERE 1=1 {where}
                    ORDER BY e.CodigoEjemplar ASC
                    LIMIT {tamano} OFFSET {(pagina - 1) * tamano}";

                using (var command = new MySqlCommand(sql, connection))
                {
                    foreach (var p in parametros)
                        command.Parameters.AddWithValue(p.Key, p.Value);

                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                        res.Add(MapEjemplar(reader));
                    connection.Close();
                }
            }
            return res;
        }

        public int ContarConFiltros(string? busqueda, string? estado)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                var (where, parametros) = ConstruirFiltros(busqueda, estado);

                string sql = $@"
                    SELECT COUNT(*) 
                    FROM Ejemplar e
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

        public Ejemplar? ObtenerPorId(int id)
        {
            Ejemplar? e = null;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"
                    SELECT e.CodigoEjemplar, e.ISBN, e.Localizacion, e.Estado,
                           l.Titulo AS LibroTitulo
                    FROM Ejemplar e
                    INNER JOIN Libro l ON e.ISBN = l.ISBN
                    WHERE e.CodigoEjemplar = @id";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                        e = MapEjemplar(reader);
                    connection.Close();
                }
            }
            return e;
        }

        public IList<Ejemplar> ObtenerPorLibro(string isbn)
        {
            var res = new List<Ejemplar>();
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"
                    SELECT e.CodigoEjemplar, e.ISBN, e.Localizacion, e.Estado,
                           l.Titulo AS LibroTitulo
                    FROM Ejemplar e
                    INNER JOIN Libro l ON e.ISBN = l.ISBN
                    WHERE e.ISBN = @isbn
                    ORDER BY e.CodigoEjemplar";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@isbn", isbn);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                        res.Add(MapEjemplar(reader));
                    connection.Close();
                }
            }
            return res;
        }

        public bool TienePrestamosActivos(int codigoEjemplar)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"SELECT COUNT(*) FROM Prestamo 
                               WHERE CodigoEjemplar = @codigo AND Estado = 'Activo'";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@codigo", codigoEjemplar);
                    connection.Open();
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
        }

        private (string where, Dictionary<string, object> parametros) ConstruirFiltros(
            string? busqueda, string? estado)
        {
            var where = "";
            var parametros = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                where += " AND (l.Titulo LIKE @busqueda OR e.ISBN LIKE @busqueda OR e.Localizacion LIKE @busqueda)";
                parametros["@busqueda"] = "%" + busqueda + "%";
            }
            if (!string.IsNullOrWhiteSpace(estado))
            {
                where += " AND e.Estado = @estado";
                parametros["@estado"] = estado;
            }

            return (where, parametros);
        }

        private Ejemplar MapEjemplar(MySqlDataReader reader)
        {
            var e = new Ejemplar
            {
                CodigoEjemplar = reader.GetInt32("CodigoEjemplar"),
                ISBN = reader.GetString("ISBN"),
                Localizacion = reader.IsDBNull(reader.GetOrdinal("Localizacion"))
                    ? null : reader.GetString("Localizacion"),
                Estado = reader.GetString("Estado")
            };

            if (HasColumn(reader, "LibroTitulo") && !reader.IsDBNull(reader.GetOrdinal("LibroTitulo")))
                e.LibroTitulo = reader.GetString("LibroTitulo");

            return e;
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