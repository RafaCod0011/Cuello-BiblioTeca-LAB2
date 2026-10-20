using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySqlConnector;
using Microsoft.Extensions.Configuration;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public class RepositorioLibro : RepositorioBase, IRepositorioLibro
    {
        public RepositorioLibro(IConfiguration configuration) : base(configuration) { }

        // Validacion de ISBN
        public bool ExisteIsbn(string isbn, string? isbnExcluir = null)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = "SELECT COUNT(1) FROM Libro WHERE ISBN = @isbn";
                if (!string.IsNullOrEmpty(isbnExcluir))
                    sql += " AND ISBN != @isbnExcluir";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@isbn", isbn);
                    if (!string.IsNullOrEmpty(isbnExcluir))
                        command.Parameters.AddWithValue("@isbnExcluir", isbnExcluir);

                    connection.Open();
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
        }

    
        public int Alta(Libro l)
        {
            if (ExisteIsbn(l.ISBN))
                throw new Exception("Ya existe un libro con ese ISBN.");

            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string sql = @"INSERT INTO Libro 
                            (ISBN, Titulo, NroPaginas, Editorial, AnioPublicacion, Sinopsis, Portada, IdCategoria)
                            VALUES (@isbn, @titulo, @paginas, @editorial, @anio, @sinopsis, @portada, @idCategoria)";
                        using (var command = new MySqlCommand(sql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@isbn", l.ISBN);
                            command.Parameters.AddWithValue("@titulo", l.Titulo);
                            command.Parameters.AddWithValue("@paginas", (object?)l.NroPaginas ?? DBNull.Value);
                            command.Parameters.AddWithValue("@editorial", string.IsNullOrEmpty(l.Editorial) ? DBNull.Value : l.Editorial);
                            command.Parameters.AddWithValue("@anio", (object?)l.AnioPublicacion ?? DBNull.Value);
                            command.Parameters.AddWithValue("@sinopsis", string.IsNullOrEmpty(l.Sinopsis) ? DBNull.Value : l.Sinopsis);
                            command.Parameters.AddWithValue("@portada", string.IsNullOrEmpty(l.Portada) ? DBNull.Value : l.Portada);
                            command.Parameters.AddWithValue("@idCategoria", (object?)l.IdCategoria ?? DBNull.Value);
                            res = command.ExecuteNonQuery();
                        }

                        foreach (var idAutor in l.IdAutores)
                        {
                            string sqlAutor = "INSERT IGNORE INTO Escribe (IdAutor, ISBN) VALUES (@idAutor, @isbn)";
                            using (var command = new MySqlCommand(sqlAutor, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@idAutor", idAutor);
                                command.Parameters.AddWithValue("@isbn", l.ISBN);
                                command.ExecuteNonQuery();
                            }
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

        
        public int Modificacion(Libro l)
        {
            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string sql = @"UPDATE Libro SET 
                            Titulo = @titulo,
                            NroPaginas = @paginas,
                            Editorial = @editorial,
                            AnioPublicacion = @anio,
                            Sinopsis = @sinopsis,
                            Portada = @portada,
                            IdCategoria = @idCategoria
                            WHERE ISBN = @isbn";
                        using (var command = new MySqlCommand(sql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@isbn", l.ISBN);
                            command.Parameters.AddWithValue("@titulo", l.Titulo);
                            command.Parameters.AddWithValue("@paginas", (object?)l.NroPaginas ?? DBNull.Value);
                            command.Parameters.AddWithValue("@editorial", string.IsNullOrEmpty(l.Editorial) ? DBNull.Value : l.Editorial);
                            command.Parameters.AddWithValue("@anio", (object?)l.AnioPublicacion ?? DBNull.Value);
                            command.Parameters.AddWithValue("@sinopsis", string.IsNullOrEmpty(l.Sinopsis) ? DBNull.Value : l.Sinopsis);
                            command.Parameters.AddWithValue("@portada", string.IsNullOrEmpty(l.Portada) ? DBNull.Value : l.Portada);
                            command.Parameters.AddWithValue("@idCategoria", (object?)l.IdCategoria ?? DBNull.Value);
                            res = command.ExecuteNonQuery();
                        }

                        string sqlDelete = "DELETE FROM Escribe WHERE ISBN = @isbn";
                        using (var command = new MySqlCommand(sqlDelete, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@isbn", l.ISBN);
                            command.ExecuteNonQuery();
                        }

                        foreach (var idAutor in l.IdAutores)
                        {
                            string sqlAutor = "INSERT INTO Escribe (IdAutor, ISBN) VALUES (@idAutor, @isbn)";
                            using (var command = new MySqlCommand(sqlAutor, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@idAutor", idAutor);
                                command.Parameters.AddWithValue("@isbn", l.ISBN);
                                command.ExecuteNonQuery();
                            }
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

        
        public int Baja(string isbn)
        {
            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = "DELETE FROM Libro WHERE ISBN = @isbn";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@isbn", isbn);
                    connection.Open();
                    res = command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            return res;
        }

       
        public IList<Libro> ObtenerLista(int pagina = 1, int tamPagina = 6)
        {
            var res = new List<Libro>();
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = $@"
                    SELECT l.ISBN, l.Titulo, l.NroPaginas, l.Editorial, l.AnioPublicacion, 
                           l.Sinopsis, l.Portada, l.IdCategoria,
                           c.Nombre AS CategoriaNombre,
                           (SELECT GROUP_CONCAT(CONCAT(a.Nombre, ' ', a.Apellido) SEPARATOR ', ')
                            FROM Escribe e 
                            INNER JOIN Autor a ON e.IdAutor = a.IdAutor 
                            WHERE e.ISBN = l.ISBN) AS AutoresTexto,
                           (SELECT COUNT(*) FROM Ejemplar ej WHERE ej.ISBN = l.ISBN) AS TotalEjemplares,
                           (SELECT COUNT(*) FROM Ejemplar ej WHERE ej.ISBN = l.ISBN AND ej.Estado = 'Disponible') AS EjemplaresDisponibles
                    FROM Libro l
                    LEFT JOIN Categoria c ON l.IdCategoria = c.IdCategoria
                    ORDER BY l.Titulo ASC
                    LIMIT {tamPagina} OFFSET {(pagina - 1) * tamPagina}";

                using (var command = new MySqlCommand(sql, connection))
                {
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                        res.Add(MapLibro(reader));
                    connection.Close();
                }
            }
            return res;
        }

        public int ObtenerCantidad()
        {
            int res = 0;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = "SELECT COUNT(ISBN) FROM Libro";
                using (var command = new MySqlCommand(sql, connection))
                {
                    connection.Open();
                    res = Convert.ToInt32(command.ExecuteScalar());
                    connection.Close();
                }
            }
            return res;
        }

       
        public Libro? ObtenerPorIsbn(string isbn)
        {
            Libro? l = null;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"SELECT l.ISBN, l.Titulo, l.NroPaginas, l.Editorial, l.AnioPublicacion, 
                                    l.Sinopsis, l.Portada, l.IdCategoria, c.Nombre AS CategoriaNombre,
                                    (SELECT GROUP_CONCAT(CONCAT(a.Nombre, ' ', a.Apellido) SEPARATOR ', ')
                                    FROM Escribe e 
                                    INNER JOIN Autor a ON e.IdAutor = a.IdAutor 
                                    WHERE e.ISBN = l.ISBN) AS AutoresTexto,
                                    (SELECT COUNT(*) FROM Ejemplar ej WHERE ej.ISBN = l.ISBN) AS TotalEjemplares,
                                    (SELECT COUNT(*) FROM Ejemplar ej WHERE ej.ISBN = l.ISBN AND ej.Estado = 'Disponible') AS EjemplaresDisponibles
                            FROM Libro l
                            LEFT JOIN Categoria c ON l.IdCategoria = c.IdCategoria
                            WHERE l.ISBN = @isbn";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@isbn", isbn);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                        l = MapLibro(reader);
                    connection.Close();
                }

                if (l != null)
                {
                    l.Autores = ObtenerAutores(isbn).ToList();
                    l.IdAutores = new List<int>();
                    foreach (var a in l.Autores)
                        l.IdAutores.Add(a.IdAutor);
                }
            }
            return l;
        }
        
       
        public IList<Autor> ObtenerAutores(string isbn)
        {
            var res = new List<Autor>();
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"
                    SELECT a.IdAutor, a.Nombre, a.Apellido, a.Nacionalidad
                    FROM Autor a
                    INNER JOIN Escribe e ON a.IdAutor = e.IdAutor
                    WHERE e.ISBN = @isbn
                    ORDER BY a.Apellido, a.Nombre";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@isbn", isbn);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                        res.Add(MapAutor(reader));
                    connection.Close();
                }
            }
            return res;
        }

        public IList<Libro> BuscarPorTituloOIsbn(string term)
        {
            var res = new List<Libro>();
            term = "%" + term + "%";
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"SELECT ISBN, Titulo FROM Libro 
                            WHERE Titulo LIKE @term OR ISBN LIKE @term
                            ORDER BY Titulo
                            LIMIT 20";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@term", term);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        res.Add(new Libro
                        {
                            ISBN = reader.GetString("ISBN"),
                            Titulo = reader.GetString("Titulo")
                        });
                    }
                    connection.Close();
                }
            }
            return res;
        }

        public IList<Libro> ObtenerPorAutor(int idAutor, int pagina, int tamano)
        {
            var res = new List<Libro>();
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = $@"
                    SELECT l.ISBN, l.Titulo, l.NroPaginas, l.Editorial, l.AnioPublicacion, 
                        l.Sinopsis, l.Portada, l.IdCategoria,
                        c.Nombre AS CategoriaNombre,
                        (SELECT GROUP_CONCAT(CONCAT(a.Nombre, ' ', a.Apellido) SEPARATOR ', ')
                            FROM Escribe e 
                            INNER JOIN Autor a ON e.IdAutor = a.IdAutor 
                            WHERE e.ISBN = l.ISBN) AS AutoresTexto,
                        (SELECT COUNT(*) FROM Ejemplar ej WHERE ej.ISBN = l.ISBN) AS TotalEjemplares,
                        (SELECT COUNT(*) FROM Ejemplar ej WHERE ej.ISBN = l.ISBN AND ej.Estado = 'Disponible') AS EjemplaresDisponibles
                    FROM Libro l
                    LEFT JOIN Categoria c ON l.IdCategoria = c.IdCategoria
                    INNER JOIN Escribe e2 ON l.ISBN = e2.ISBN
                    WHERE e2.IdAutor = @idAutor
                    ORDER BY l.Titulo ASC
                    LIMIT {tamano} OFFSET {(pagina - 1) * tamano}";

                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@idAutor", idAutor);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                        res.Add(MapLibro(reader));
                    connection.Close();
                }
            }
            return res;
        }

        public int ContarPorAutor(int idAutor)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"SELECT COUNT(DISTINCT l.ISBN) 
                            FROM Libro l
                            INNER JOIN Escribe e ON l.ISBN = e.ISBN
                            WHERE e.IdAutor = @idAutor";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@idAutor", idAutor);
                    connection.Open();
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

      
        public int ObtenerCantidadEjemplares(string isbn)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = "SELECT COUNT(CodigoEjemplar) FROM Ejemplar WHERE ISBN = @isbn";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@isbn", isbn);
                    connection.Open();
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public int ObtenerCantidadEjemplaresDisponibles(string isbn)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = "SELECT COUNT(CodigoEjemplar) FROM Ejemplar WHERE ISBN = @isbn AND Estado = 'Disponible'";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@isbn", isbn);
                    connection.Open();
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        
        private Libro MapLibro(MySqlDataReader reader)
        {
            var l = new Libro
            {
                ISBN = reader.GetString("ISBN"),
                Titulo = reader.GetString("Titulo"),
                NroPaginas = reader.IsDBNull(reader.GetOrdinal("NroPaginas"))
                    ? null : reader.GetInt32("NroPaginas"),
                Editorial = reader.IsDBNull(reader.GetOrdinal("Editorial"))
                    ? null : reader.GetString("Editorial"),
                AnioPublicacion = reader.IsDBNull(reader.GetOrdinal("AnioPublicacion"))
                    ? null : reader.GetInt32("AnioPublicacion"),
                Sinopsis = reader.IsDBNull(reader.GetOrdinal("Sinopsis"))
                    ? null : reader.GetString("Sinopsis"),
                Portada = reader.IsDBNull(reader.GetOrdinal("Portada"))
                    ? null : reader.GetString("Portada"),
                IdCategoria = reader.IsDBNull(reader.GetOrdinal("IdCategoria"))
                    ? null : reader.GetInt32("IdCategoria")
            };

            if (HasColumn(reader, "CategoriaNombre") && !reader.IsDBNull(reader.GetOrdinal("CategoriaNombre")))
                l.CategoriaNombre = reader.GetString("CategoriaNombre");

            if (HasColumn(reader, "AutoresTexto") && !reader.IsDBNull(reader.GetOrdinal("AutoresTexto")))
                l.AutoresTexto = reader.GetString("AutoresTexto");

            if (HasColumn(reader, "TotalEjemplares") && !reader.IsDBNull(reader.GetOrdinal("TotalEjemplares")))
                l.TotalEjemplares = reader.GetInt32("TotalEjemplares");

            if (HasColumn(reader, "EjemplaresDisponibles") && !reader.IsDBNull(reader.GetOrdinal("EjemplaresDisponibles")))
                l.EjemplaresDisponibles = reader.GetInt32("EjemplaresDisponibles");

            return l;
        }

        private Autor MapAutor(MySqlDataReader reader)
        {
            return new Autor
            {
                IdAutor = reader.GetInt32("IdAutor"),
                Nombre = reader.GetString("Nombre"),
                Apellido = reader.GetString("Apellido"),
                Nacionalidad = reader.IsDBNull(reader.GetOrdinal("Nacionalidad"))
                    ? null : reader.GetString("Nacionalidad")
            };
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