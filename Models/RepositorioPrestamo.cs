using System;
using System.Collections.Generic;
using System.Data;
using MySqlConnector;
using Microsoft.Extensions.Configuration;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public class RepositorioPrestamo : RepositorioBase, IRepositorioPrestamo
    {
        public RepositorioPrestamo(IConfiguration configuration) : base(configuration) { }

        public int Alta(Prestamo p)
        {
            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string sql = @"INSERT INTO Prestamo 
                            (IdUsuario, CodigoEjemplar, FechaPrestamo, FechaVencimiento, Estado, IdUsuarioBibliotecario)
                            VALUES (@idUsuario, @codigoEjemplar, @fechaPrestamo, @fechaVencimiento, @estado, @idBibliotecario);
                            SELECT LAST_INSERT_ID();";

                        using (var command = new MySqlCommand(sql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@idUsuario", p.IdUsuario);
                            command.Parameters.AddWithValue("@codigoEjemplar", p.CodigoEjemplar);
                            command.Parameters.AddWithValue("@fechaPrestamo", p.FechaPrestamo);
                            command.Parameters.AddWithValue("@fechaVencimiento", p.FechaVencimiento);
                            command.Parameters.AddWithValue("@estado", p.Estado);
                            command.Parameters.AddWithValue("@idBibliotecario", p.IdUsuarioBibliotecario);
                            res = Convert.ToInt32(command.ExecuteScalar());
                            p.IdPrestamo = res;
                        }

                        string sqlEjemplar = "UPDATE Ejemplar SET Estado = 'Prestado' WHERE CodigoEjemplar = @codigo";
                        using (var command = new MySqlCommand(sqlEjemplar, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@codigo", p.CodigoEjemplar);
                            command.ExecuteNonQuery();
                        }

                        string sqlReserva = @"
                            UPDATE Reserva 
                            SET Estado = 'Cumplida' 
                            WHERE IdUsuario = @idUsuario 
                              AND ISBN = (SELECT ISBN FROM Ejemplar WHERE CodigoEjemplar = @codigo)
                              AND Estado = 'Pendiente'";

                        using (var command = new MySqlCommand(sqlReserva, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@idUsuario", p.IdUsuario);
                            command.Parameters.AddWithValue("@codigo", p.CodigoEjemplar);
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            return res;
        }

        public int Baja(int id)
        {
            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = "DELETE FROM Prestamo WHERE IdPrestamo = @id";
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

        public int Modificacion(Prestamo p)
        {
            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"UPDATE Prestamo 
                               SET IdUsuario = @idUsuario,
                                   CodigoEjemplar = @codigoEjemplar,
                                   FechaPrestamo = @fechaPrestamo,
                                   FechaVencimiento = @fechaVencimiento,
                                   Estado = @estado,
                                   IdUsuarioBibliotecario = @idBibliotecario
                               WHERE IdPrestamo = @id";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@idUsuario", p.IdUsuario);
                    command.Parameters.AddWithValue("@codigoEjemplar", p.CodigoEjemplar);
                    command.Parameters.AddWithValue("@fechaPrestamo", p.FechaPrestamo);
                    command.Parameters.AddWithValue("@fechaVencimiento", p.FechaVencimiento);
                    command.Parameters.AddWithValue("@estado", p.Estado);
                    command.Parameters.AddWithValue("@idBibliotecario", p.IdUsuarioBibliotecario);
                    command.Parameters.AddWithValue("@id", p.IdPrestamo);
                    connection.Open();
                    res = command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            return res;
        }

        public void RegistrarDevolucion(int idPrestamo, DateTime fechaDevolucion)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        //Obtener el préstamo para saber el ejemplar
                        int codigoEjemplar = 0;
                        string sqlSelect = "SELECT CodigoEjemplar FROM Prestamo WHERE IdPrestamo = @id";
                        using (var command = new MySqlCommand(sqlSelect, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@id", idPrestamo);
                            var result = command.ExecuteScalar();
                            if (result == null)
                                throw new Exception("Préstamo no encontrado");
                            codigoEjemplar = Convert.ToInt32(result);
                        }

                        // Actualizar el préstamo
                        string sqlUpdate = @"
                            UPDATE Prestamo 
                            SET FechaDevolucion = @fechaDevolucion, Estado = 'Devuelto'
                            WHERE IdPrestamo = @id";

                        using (var command = new MySqlCommand(sqlUpdate, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@fechaDevolucion", fechaDevolucion);
                            command.Parameters.AddWithValue("@id", idPrestamo);
                            command.ExecuteNonQuery();
                        }

                        // Devolver el ejemplar a Disponible
                        string sqlEjemplar = "UPDATE Ejemplar SET Estado = 'Disponible' WHERE CodigoEjemplar = @codigo";
                        using (var command = new MySqlCommand(sqlEjemplar, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@codigo", codigoEjemplar);
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public IList<Prestamo> ObtenerLista(int pagina = 1, int tamPagina = 10)
        {
            return BuscarConFiltros(null, null, false, pagina, tamPagina);
        }

        public int ObtenerCantidad()
        {
            return ContarConFiltros(null, null, false);
        }

        public IList<Prestamo> BuscarConFiltros(string? busqueda, string? estado, bool soloVencidos, int pagina, int tamano)
        {
            var res = new List<Prestamo>();
            using (var connection = new MySqlConnection(connectionString))
            {
                var (where, parametros) = ConstruirFiltros(busqueda, estado, soloVencidos);

                string sql = $@"
                    SELECT p.IdPrestamo, p.IdUsuario, p.CodigoEjemplar, 
                           p.FechaPrestamo, p.FechaVencimiento, p.FechaDevolucion,
                           p.Estado, p.IdUsuarioBibliotecario,
                           u1.Nombre AS SocioNombre, u1.Apellido AS SocioApellido, u1.Dni AS SocioDni,
                           u2.Nombre AS BiblioNombre, u2.Apellido AS BiblioApellido,
                           l.ISBN, l.Titulo AS LibroTitulo,
                           e.Localizacion AS EjemplarLocalizacion
                    FROM Prestamo p
                    INNER JOIN Usuario u1 ON p.IdUsuario = u1.IdUsuario
                    INNER JOIN Usuario u2 ON p.IdUsuarioBibliotecario = u2.IdUsuario
                    INNER JOIN Ejemplar e ON p.CodigoEjemplar = e.CodigoEjemplar
                    INNER JOIN Libro l ON e.ISBN = l.ISBN
                    WHERE 1=1 {where}
                    ORDER BY 
                        CASE WHEN p.Estado = 'Activo' THEN 0 ELSE 1 END,
                        p.FechaVencimiento ASC
                    LIMIT {tamano} OFFSET {(pagina - 1) * tamano}";

                using (var command = new MySqlCommand(sql, connection))
                {
                    foreach (var p in parametros)
                        command.Parameters.AddWithValue(p.Key, p.Value);

                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                        res.Add(MapPrestamo(reader));
                    connection.Close();
                }
            }
            return res;
        }

        public int ContarConFiltros(string? busqueda, string? estado, bool soloVencidos)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                var (where, parametros) = ConstruirFiltros(busqueda, estado, soloVencidos);

                string sql = $@"
                    SELECT COUNT(*) 
                    FROM Prestamo p
                    INNER JOIN Usuario u1 ON p.IdUsuario = u1.IdUsuario
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

        public IList<Prestamo> ObtenerPorSocio(int idSocio, int pagina, int tamano)
        {
            return BuscarConFiltros(null, null, false, pagina, tamano)
                .Where(p => p.IdUsuario == idSocio).ToList();
        }

        public int ContarPorSocio(int idSocio)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = "SELECT COUNT(*) FROM Prestamo WHERE IdUsuario = @id";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", idSocio);
                    connection.Open();
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public Prestamo? ObtenerPorId(int id)
        {
            Prestamo? p = null;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"
                    SELECT p.IdPrestamo, p.IdUsuario, p.CodigoEjemplar, 
                           p.FechaPrestamo, p.FechaVencimiento, p.FechaDevolucion,
                           p.Estado, p.IdUsuarioBibliotecario,
                           u1.Nombre AS SocioNombre, u1.Apellido AS SocioApellido, u1.Dni AS SocioDni,
                           u2.Nombre AS BiblioNombre, u2.Apellido AS BiblioApellido,
                           l.ISBN, l.Titulo AS LibroTitulo,
                           e.Localizacion AS EjemplarLocalizacion
                    FROM Prestamo p
                    INNER JOIN Usuario u1 ON p.IdUsuario = u1.IdUsuario
                    INNER JOIN Usuario u2 ON p.IdUsuarioBibliotecario = u2.IdUsuario
                    INNER JOIN Ejemplar e ON p.CodigoEjemplar = e.CodigoEjemplar
                    INNER JOIN Libro l ON e.ISBN = l.ISBN
                    WHERE p.IdPrestamo = @id";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                        p = MapPrestamo(reader);
                    connection.Close();
                }
            }
            return p;
        }

        public int ContarPrestamosActivosPorSocio(int idSocio)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = "SELECT COUNT(*) FROM Prestamo WHERE IdUsuario = @id AND Estado = 'Activo'";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", idSocio);
                    connection.Open();
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public int ContarMultasPendientesPorSocio(int idSocio)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"
                    SELECT COUNT(*) FROM Multa m
                    INNER JOIN Prestamo p ON m.IdPrestamo = p.IdPrestamo
                    WHERE p.IdUsuario = @id AND m.Pagada = FALSE";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", idSocio);
                    connection.Open();
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public int ContarPrestamosVencidosPorSocio(int idSocio)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"
                    SELECT COUNT(*) FROM Prestamo 
                    WHERE IdUsuario = @id 
                      AND Estado = 'Activo' 
                      AND FechaVencimiento < CURDATE()";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", idSocio);
                    connection.Open();
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public bool TienePrestamoActivoDelLibro(int idSocio, string isbn)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"
                    SELECT COUNT(*) FROM Prestamo p
                    INNER JOIN Ejemplar e ON p.CodigoEjemplar = e.CodigoEjemplar
                    WHERE p.IdUsuario = @id 
                      AND p.Estado = 'Activo' 
                      AND e.ISBN = @isbn";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", idSocio);
                    command.Parameters.AddWithValue("@isbn", isbn);
                    connection.Open();
                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }
        }

        public bool TienePrestamoActivoDelEjemplar(int codigoEjemplar)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = "SELECT COUNT(*) FROM Prestamo WHERE CodigoEjemplar = @codigo AND Estado = 'Activo'";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@codigo", codigoEjemplar);
                    connection.Open();
                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }
        }

        private (string where, Dictionary<string, object> parametros) ConstruirFiltros(
            string? busqueda, string? estado, bool soloVencidos)
        {
            var where = "";
            var parametros = new Dictionary<string, object>();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                where += @" AND (u1.Nombre LIKE @busqueda 
                            OR u1.Apellido LIKE @busqueda 
                            OR u1.Dni LIKE @busqueda 
                            OR l.Titulo LIKE @busqueda 
                            OR l.ISBN LIKE @busqueda)";
                parametros["@busqueda"] = "%" + busqueda + "%";
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                if (estado == "Vencido")
                {
                    where += " AND p.Estado = 'Activo' AND p.FechaVencimiento < CURDATE()";
                }
                else
                {
                    where += " AND p.Estado = @estado";
                    parametros["@estado"] = estado;
                }
            }

            if (soloVencidos)
            {
                where += " AND p.Estado = 'Activo' AND p.FechaVencimiento < CURDATE()";
            }

            return (where, parametros);
        }

        private Prestamo MapPrestamo(MySqlDataReader reader)
        {
            var p = new Prestamo
            {
                IdPrestamo = reader.GetInt32("IdPrestamo"),
                IdUsuario = reader.GetInt32("IdUsuario"),
                CodigoEjemplar = reader.GetInt32("CodigoEjemplar"),
                FechaPrestamo = reader.GetDateTime("FechaPrestamo"),
                FechaVencimiento = reader.GetDateTime("FechaVencimiento"),
                FechaDevolucion = reader.IsDBNull(reader.GetOrdinal("FechaDevolucion"))
                    ? null : reader.GetDateTime("FechaDevolucion"),
                Estado = reader.GetString("Estado"),
                IdUsuarioBibliotecario = reader.GetInt32("IdUsuarioBibliotecario")
            };

            // Relaciones para mostrar en vistas
            if (HasColumn(reader, "SocioNombre"))
            {
                p.Socio = new Usuario
                {
                    Id = p.IdUsuario,
                    Nombre = reader.GetString("SocioNombre"),
                    Apellido = reader.GetString("SocioApellido"),
                    Dni = reader.IsDBNull(reader.GetOrdinal("SocioDni"))
                        ? "" : reader.GetString("SocioDni")
                };
            }

            if (HasColumn(reader, "BiblioNombre"))
            {
                p.Bibliotecario = new Usuario
                {
                    Id = p.IdUsuarioBibliotecario,
                    Nombre = reader.GetString("BiblioNombre"),
                    Apellido = reader.GetString("BiblioApellido")
                };
            }

            if (HasColumn(reader, "LibroTitulo"))
            {
                p.Ejemplar = new Ejemplar
                {
                    CodigoEjemplar = p.CodigoEjemplar,
                    ISBN = reader.GetString("ISBN"),
                    LibroTitulo = reader.GetString("LibroTitulo"),
                    Localizacion = reader.IsDBNull(reader.GetOrdinal("EjemplarLocalizacion"))
                        ? null : reader.GetString("EjemplarLocalizacion")
                };
            }

            return p;
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