using BibliotecaAPI.Entidades;
using System.ComponentModel.DataAnnotations;

namespace BibliotecaAPI.DTOs
{
    public class LibroDTO
    {
        public int Id { get; set; }
        [Required]
        public required string Titulo { get; set; }
    }
}
