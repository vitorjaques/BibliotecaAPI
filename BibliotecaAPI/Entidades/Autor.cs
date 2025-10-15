using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Entidades
{
    public class Autor
    {
        public int Id { get; set; }
        [Required(ErrorMessage ="El campo {0} es requerido")]
        public required string Nombre { get; set; }
        public required string Apellido { get; set; }
        public string? Identificacion { get; set; }
        [Unicode(false)]
        public string? Foto { get; set; }
        public List<AutorLibro> Libros { get; set; } = [];
    }
}
