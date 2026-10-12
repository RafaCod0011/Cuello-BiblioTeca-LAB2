using System;
using System.Collections.Generic;
using System.Data;
using MySqlConnector;
using Microsoft.Extensions.Configuration;

namespace Cuello_BiblioTeca_LAB2.Models
{
    public class RepositorioUsuario : RepositorioBase, IRepositorioUsuario
    {
        public RepositorioUsuario(IConfiguration configuration) : base(configuration) { }

        public int Alta(Usuario u)
        {
            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"INSERT INTO Usuario 
                    (Nombre, Apellido, Dni, Direccion, Telefono, Email, Clave, Avatar, Rol) 
                    VALUES (@nombre, @apellido, @dni, @direccion, @telefono, @email, @clave, @avatar, @rol);
                    SELECT LAST_INSERT_ID();";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@nombre", u.Nombre);
                    command.Parameters.AddWithValue("@apellido", u.Apellido);
                    command.Parameters.AddWithValue("@dni", u.Dni);
                    command.Parameters.AddWithValue("@direccion", string.IsNullOrEmpty(u.Direccion) ? (object)DBNull.Value : u.Direccion);
                    command.Parameters.AddWithValue("@telefono", string.IsNullOrEmpty(u.Telefono) ? (object)DBNull.Value : u.Telefono);
                    command.Parameters.AddWithValue("@email", u.Email);
                    command.Parameters.AddWithValue("@clave", u.Clave);
                    command.Parameters.AddWithValue("@avatar", string.IsNullOrEmpty(u.Avatar) ? (object)DBNull.Value : u.Avatar);
                    command.Parameters.AddWithValue("@rol", u.Rol);
                    connection.Open();
                    res = Convert.ToInt32(command.ExecuteScalar());
                    u.Id = res;
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
                string sql = "DELETE FROM Usuario WHERE IdUsuario = @id";
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

        public int Modificacion(Usuario u)
        {
            int res = -1;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"UPDATE Usuario 
                    SET Nombre = @nombre, Apellido = @apellido, Dni = @dni, 
                        Direccion = @direccion, Telefono = @telefono,
                        Email = @email, Clave = @clave, Avatar = @avatar, Rol = @rol
                    WHERE IdUsuario = @id";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@nombre", u.Nombre);
                    command.Parameters.AddWithValue("@apellido", u.Apellido);
                    command.Parameters.AddWithValue("@dni", u.Dni);
                    command.Parameters.AddWithValue("@direccion", string.IsNullOrEmpty(u.Direccion) ? (object)DBNull.Value : u.Direccion);
                    command.Parameters.AddWithValue("@telefono", string.IsNullOrEmpty(u.Telefono) ? (object)DBNull.Value : u.Telefono);
                    command.Parameters.AddWithValue("@email", u.Email);
                    command.Parameters.AddWithValue("@clave", u.Clave);
                    command.Parameters.AddWithValue("@avatar", string.IsNullOrEmpty(u.Avatar) ? (object)DBNull.Value : u.Avatar);
                    command.Parameters.AddWithValue("@rol", u.Rol);
                    command.Parameters.AddWithValue("@id", u.Id);
                    connection.Open();
                    res = command.ExecuteNonQuery();
                    connection.Close();
                }
            }
            return res;
        }

        public IList<Usuario> ObtenerLista(int pagina = 1, int tamPagina = 10)
        {
            var res = new List<Usuario>();
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = $@"SELECT IdUsuario, Nombre, Apellido, Dni, Direccion, Telefono, 
                                       Email, Clave, Avatar, Rol
                                FROM Usuario
                                ORDER BY Apellido, Nombre
                                LIMIT {tamPagina} OFFSET {(pagina - 1) * tamPagina}";
                using (var command = new MySqlCommand(sql, connection))
                {
                    connection.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        res.Add(MapUsuario(reader));
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
                string sql = "SELECT COUNT(IdUsuario) FROM Usuario";
                using (var command = new MySqlCommand(sql, connection))
                {
                    connection.Open();
                    res = Convert.ToInt32(command.ExecuteScalar());
                    connection.Close();
                }
            }
            return res;
        }

        public Usuario? ObtenerPorId(int id)
        {
            Usuario? u = null;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"SELECT IdUsuario, Nombre, Apellido, Dni, Direccion, Telefono, 
                                      Email, Clave, Avatar, Rol 
                               FROM Usuario WHERE IdUsuario = @id";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                        u = MapUsuario(reader);
                    connection.Close();
                }
            }
            return u;
        }

        public Usuario? ObtenerPorEmail(string email)
        {
            Usuario? u = null;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"SELECT IdUsuario, Nombre, Apellido, Dni, Direccion, Telefono, 
                                      Email, Clave, Avatar, Rol 
                               FROM Usuario WHERE Email = @email";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@email", email);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                        u = MapUsuario(reader);
                    connection.Close();
                }
            }
            return u;
        }

        public Usuario? ObtenerPorDni(string dni)
        {
            Usuario? u = null;
            using (var connection = new MySqlConnection(connectionString))
            {
                string sql = @"SELECT IdUsuario, Nombre, Apellido, Dni, Direccion, Telefono, 
                                      Email, Clave, Avatar, Rol 
                               FROM Usuario WHERE Dni = @dni";
                using (var command = new MySqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@dni", dni);
                    connection.Open();
                    var reader = command.ExecuteReader();
                    if (reader.Read())
                        u = MapUsuario(reader);
                    connection.Close();
                }
            }
            return u;
        }

        private Usuario MapUsuario(MySqlDataReader reader)
        {
            return new Usuario
            {
                Id = reader.GetInt32("IdUsuario"),
                Nombre = reader.GetString("Nombre"),
                Apellido = reader.GetString("Apellido"),
                Dni = reader.GetString("Dni"),
                Direccion = reader.IsDBNull(reader.GetOrdinal("Direccion")) ? null : reader.GetString("Direccion"),
                Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? null : reader.GetString("Telefono"),
                Email = reader.GetString("Email"),
                Clave = reader.GetString("Clave"),
                Avatar = reader.IsDBNull(reader.GetOrdinal("Avatar")) ? null : reader.GetString("Avatar"),
                Rol = reader.GetInt32("Rol")
            };
        }
    }
}