using BibliotecaAPI.Entidades;
using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.DTOs
{
    public class AutorCreacionDTO
    {
        [Required(ErrorMessage = "El campo {0} es requerido")]
        public required string Nombre { get; set; }
        public required string Apellido { get; set; }
        public string? Identificacion { get; set; }
        public List<LibroCreacionDTO> Libros { get; set; } = [];
    }
}
