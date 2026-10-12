using System.Security.Claims;

namespace Cuello_BiblioTeca_LAB2.Helpers
{
    public static class UsuarioExtensions
    {
        public static int UsuarioId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }
}