using AutoMapper;
using Azure;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Controllers
{
    [ApiController]
    [Route("api/autores")]
    [Authorize(Policy = "esadmin")]
    public class AutoresController: ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IAlmacenadorArchivos almacenadorArchivos;
        private const string contenedor = "autores";
   

        public AutoresController(ApplicationDbContext context, IMapper mapper, IAlmacenadorArchivos almacenadorArchivos)
        {
            this.context = context;
            this.mapper = mapper;
            this.almacenadorArchivos = almacenadorArchivos;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IEnumerable<AutorDTO>> SelectAutores()
        {
            var autores = await context.Autores.ToListAsync();
            var autoresDTO = mapper.Map<IEnumerable<AutorDTO>>(autores);
            return autoresDTO;
        }

        [HttpGet("{id:int}", Name ="ObtenerAutor")]
        public async Task<ActionResult<AutorConLibrosDTO>> SelectAutorByID(int id)
        {
            var autor = await context.Autores
                .Include(x => x.Libros)
                .ThenInclude(x => x.Libro)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (autor is null)
            {
                return NotFound();
            }

            var autorDTO = mapper.Map<AutorConLibrosDTO>(autor);

            return Ok(autorDTO);
        }


        [HttpPost]
        public async Task<ActionResult> CreateAutor(AutorCreacionDTO autorCreacion)
        {
            var autor = mapper.Map<Autor>(autorCreacion);
            context.Autores.Add(autor);
            await context.SaveChangesAsync();
            var autorDTO = mapper.Map<AutorDTO>(autor);
            return CreatedAtRoute("ObtenerAutor", new { id = autor.Id }, autorDTO);
        }

        [HttpPost("con-foto")]
        [AllowAnonymous]
        public async Task<ActionResult> CreateAutorConFoto([FromForm] AutorCreacionDTOConFoto autorCreacionConFoto)
        {
            var autor = mapper.Map<Autor>(autorCreacionConFoto);

            if (autorCreacionConFoto.Foto is not null)
            {
                var url = await almacenadorArchivos.Almacenar(contenedor, autorCreacionConFoto.Foto);
                autor.Foto = url;
            }

            context.Autores.Add(autor);
            await context.SaveChangesAsync();
            var autorDTO = mapper.Map<AutorDTO>(autor);
            return CreatedAtRoute("ObtenerAutor", new { id = autor.Id }, autorDTO);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<Autor>> UpdateAutor(int id, [FromForm] AutorCreacionDTOConFoto autorCreacionDTOConFoto)
        {
            var existeAutor = await context.Autores.AnyAsync(x => x.Id == id);

            if (!existeAutor)
            {
                return NotFound();
            }

            var autor = mapper.Map<Autor>(autorCreacionDTOConFoto);
            autor.Id = id;

            if ( autorCreacionDTOConFoto.Foto is not null) 
            {
                var fotoActual = await context.Autores
                    .Where(x => x.Id == id).Select(x => x.Foto).FirstAsync();

                var url = await almacenadorArchivos.Editar(fotoActual, contenedor, autorCreacionDTOConFoto.Foto);
                autor.Foto = url;
            }

            context.Update(autor);
            await context.SaveChangesAsync();
            return NoContent();
        }



        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteAutor(int id)
        {
            var autor = await context.Autores.FirstOrDefaultAsync(x => x.Id == id);

            if (autor is null)
            { 
                return NotFound();
            }

            context.Remove(autor);
            await context.SaveChangesAsync();
            await almacenadorArchivos.Borrar(autor.Foto, contenedor);
  
            return NoContent();
        }
    }
}
