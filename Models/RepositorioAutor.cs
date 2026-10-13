using System;
using System.Collections.Generic;
using System.Data;
using MySqlConnector;
using Microsoft.Extensions.Configuration;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public class RepositorioAutor : RepositorioBase, IRepositorioAutor
    {
        public RepositorioAutor(IConfiguration configuration) : base(configuration) { }

        public int Alta(Autor a)
        {
            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"INSERT INTO Autor (Nombre, Apellido, Nacionalidad) 
                               VALUES (@nombre, @apellido, @nacionalidad);
                               SELECT LAST_INSERT_ID();";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@nombre", a.Nombre);
                    command.Parameters.AddWithValue("@apellido", a.Apellido);
                    command.Parameters.AddWithValue("@nacionalidad",
                        string.IsNullOrEmpty(a.Nacionalidad) ? (object)DBNull.Value : a.Nacionalidad);
                    connection.Open();
                    res = Convert.ToInt32(command.ExecuteScalar());
                    a.IdAutor = res;
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
                string sql = "DELETE FROM Autor WHERE IdAutor = @id";
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

        public int Modificacion(Autor a)
        {
            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"UPDATE Autor 
                               SET Nombre = @nombre, Apellido = @apellido, Nacionalidad = @nacionalidad
                               WHERE IdAutor = @id";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@nombre", a.Nombre);
                    command.Parameters.AddWithValue("@apellido", a.Apellido);
                    command.Parameters.AddWithValue("@nacionalidad",
                        string.IsNullOrEmpty(a.Nacionalidad) ? (object)DBNull.Value : a.Nacionalidad);
                    command.Parameters.AddWithValue("@id", a.IdAutor);
                    connection.Open();
                    res = command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            return res;
        }

        public IList<Autor> ObtenerLista(int pagina = 1, int tamPagina = 10)
        {
            var res = new List<Autor>();
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = $@"SELECT IdAutor, Nombre, Apellido, Nacionalidad
                                FROM Autor
                                ORDER BY Apellido, Nombre
                                LIMIT {tamPagina} OFFSET {(pagina - 1) * tamPagina}";
                using (var command = new MySqlCommand(sql, connection))
                {
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        res.Add(MapAutor(reader));
                    }
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
                string sql = "SELECT COUNT(IdAutor) FROM Autor";
                using (var command = new MySqlCommand(sql, connection))
                {
                    connection.Open();
                    res = Convert.ToInt32(command.ExecuteScalar());
                    connection.Close();
                }
            }
            return res;
        }

        public Autor? ObtenerPorId(int id)
        {
            Autor? a = null;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = "SELECT IdAutor, Nombre, Apellido, Nacionalidad FROM Autor WHERE IdAutor = @id";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                        a = MapAutor(reader);
                    connection.Close();
                }
            }
            return a;
        }

        public IList<Autor> BuscarPorNombre(string nombre)
        {
            var res = new List<Autor>();
            nombre = "%" + nombre + "%";
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"SELECT IdAutor, Nombre, Apellido, Nacionalidad 
                               FROM Autor 
                               WHERE Nombre LIKE @nombre OR Apellido LIKE @nombre
                               ORDER BY Apellido, Nombre
                               LIMIT 20";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@nombre", nombre);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        res.Add(MapAutor(reader));
                    }
                    connection.Close();
                }
            }
            return res;
        }

        private Autor MapAutor(MySqlDataReader reader)
        {
            return new Autor
            {
                IdAutor = reader.GetInt32("IdAutor"),
                Nombre = reader.GetString("Nombre"),
                Apellido = reader.GetString("Apellido"),
                Nacionalidad = reader.IsDBNull(reader.GetOrdinal("Nacionalidad"))
                    ? null
                    : reader.GetString("Nacionalidad")
            };
        }
    }
}