namespace Cuello_BiblioTeca_LAB2.Models
{
    public interface IRepositorioUsuario : IRepositorio<Usuario>
    {
        Usuario? ObtenerPorEmail(string email);
        Usuario? ObtenerPorDni(string dni);
    }
}