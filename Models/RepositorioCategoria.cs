using System;
using System.Collections.Generic;
using System.Data;
using MySqlConnector;
using Microsoft.Extensions.Configuration;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public class RepositorioCategoria : RepositorioBase, IRepositorioCategoria
    {
        public RepositorioCategoria(IConfiguration configuration) : base(configuration) { }

        public int Alta(Categoria c)
        {
            if (ObtenerPorNombre(c.Nombre) != null)
                throw new Exception("Ya existe una categoría con ese nombre.");

            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"INSERT INTO Categoria (Nombre, Descripcion) 
                               VALUES (@nombre, @descripcion);
                               SELECT LAST_INSERT_ID();";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@nombre", c.Nombre);
                    command.Parameters.AddWithValue("@descripcion",
                        string.IsNullOrEmpty(c.Descripcion) ? (object)DBNull.Value : c.Descripcion);
                    connection.Open();
                    res = Convert.ToInt32(command.ExecuteScalar());
                    c.IdCategoria = res;
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
                string sql = "DELETE FROM Categoria WHERE IdCategoria = @id";
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

        public int Modificacion(Categoria c)
        {
            var existente = ObtenerPorNombre(c.Nombre);
            if (existente != null && existente.IdCategoria != c.IdCategoria)
                throw new Exception("Ya existe otra categoría con ese nombre.");

            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"UPDATE Categoria 
                               SET Nombre = @nombre, Descripcion = @descripcion
                               WHERE IdCategoria = @id";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@nombre", c.Nombre);
                    command.Parameters.AddWithValue("@descripcion",
                        string.IsNullOrEmpty(c.Descripcion) ? (object)DBNull.Value : c.Descripcion);
                    command.Parameters.AddWithValue("@id", c.IdCategoria);
                    connection.Open();
                    res = command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            return res;
        }

        public IList<Categoria> ObtenerLista(int pagina = 1, int tamPagina = 10)
        {
            var res = new List<Categoria>();
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = $@"SELECT IdCategoria, Nombre, Descripcion
                                FROM Categoria
                                ORDER BY Nombre
                                LIMIT {tamPagina} OFFSET {(pagina - 1) * tamPagina}";
                using (var command = new MySqlCommand(sql, connection))
                {
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        res.Add(MapCategoria(reader));
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
                string sql = "SELECT COUNT(IdCategoria) FROM Categoria";
                using (var command = new MySqlCommand(sql, connection))
                {
                    connection.Open();
                    res = Convert.ToInt32(command.ExecuteScalar());
                    connection.Close();
                }
            }
            return res;
        }

        public Categoria? ObtenerPorId(int id)
        {
            Categoria? c = null;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = "SELECT IdCategoria, Nombre, Descripcion FROM Categoria WHERE IdCategoria = @id";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                        c = MapCategoria(reader);
                    connection.Close();
                }
            }
            return c;
        }

        public Categoria? ObtenerPorNombre(string nombre)
        {
            Categoria? c = null;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = "SELECT IdCategoria, Nombre, Descripcion FROM Categoria WHERE Nombre = @nombre";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@nombre", nombre);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                        c = MapCategoria(reader);
                    connection.Close();
                }
            }
            return c;
        }

        public IList<Categoria> BuscarPorNombre(string nombre)
        {
            var res = new List<Categoria>();
            nombre = "%" + nombre + "%";
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"SELECT IdCategoria, Nombre, Descripcion 
                               FROM Categoria 
                               WHERE Nombre LIKE @nombre
                               ORDER BY Nombre
                               LIMIT 20";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@nombre", nombre);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        res.Add(MapCategoria(reader));
                    }
                    connection.Close();
                }
            }
            return res;
        }

        private Categoria MapCategoria(MySqlDataReader reader)
        {
            return new Categoria
            {
                IdCategoria = reader.GetInt32("IdCategoria"),
                Nombre = reader.GetString("Nombre"),
                Descripcion = reader.IsDBNull(reader.GetOrdinal("Descripcion"))
                    ? null
                    : reader.GetString("Descripcion")
            };
        }
    }
}