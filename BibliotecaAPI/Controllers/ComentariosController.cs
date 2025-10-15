using AutoMapper;
using Azure;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Controllers
{
    [ApiController]
    [Route("api/libros/{libroId:int}/comentarios")]
    [Authorize]
    public class ComentariosController: ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;
        private readonly IServicioUsuarios servicioUsuario;

        public ComentariosController( ApplicationDbContext context, IMapper mapper, IServicioUsuarios servicioUsuario)
        {
            this.context = context;
            this.mapper = mapper;
            this.servicioUsuario = servicioUsuario;
        }

        [HttpGet]
        public async Task<ActionResult<List<ComentarioDTO>>> SelectComentarios(int libroId)
        {
            var existeLibro = await context.Libros.AnyAsync(x => x.Id == libroId);

            if (!existeLibro)
            {
                return NotFound();
            }

            var comentarios = await context.Comentarios
                                    .Include(x => x.Usuario)
                                    .Where(x => x.LibroId == libroId)
                                    .OrderByDescending( x => x.FechaPublicacion)
                                    .ToListAsync();
            return mapper.Map<List<ComentarioDTO>>(comentarios);
        }

        [HttpGet("{id:Guid}", Name = "ObtenerComentario")]
        public async Task<ActionResult<ComentarioDTO>> SelectComentarioByID(Guid id)
        {
            var comentario = await context.Comentarios
                                    .Include( x => x.Usuario)
                                    .FirstOrDefaultAsync(x => x.Id == id);

            if(comentario is null)
            {
                return NotFound();
            }

            return mapper.Map<ComentarioDTO>(comentario);
        }

        [HttpPost]
        public async Task<ActionResult> InsertComentario(int libroId, ComentarioCreacionDTO comentarioCreacionDTO)
        {
            var existeLibro = await context.Libros.AnyAsync(x => x.Id == libroId);

            if (!existeLibro)
            {
                return NotFound();
            }

            var usuario = await servicioUsuario.ObtenerUsuario();

            if (usuario == null) { return NotFound(); }


            var comentario = mapper.Map<Comentario>(comentarioCreacionDTO);
            comentario.LibroId = libroId;
            comentario.FechaPublicacion = DateTime.UtcNow;
            comentario.UsuarioID = usuario.Id;
            context.Add(comentario);
            await context.SaveChangesAsync();

            var comentarioDTO = mapper.Map<ComentarioDTO>(comentario);

            return CreatedAtRoute("ObtenerComentario", new { id = comentario.Id, libroId }, comentarioDTO);
        }

        [HttpPatch("{id}")]
        public async Task<ActionResult> UpdatePatch(Guid id, int libroId ,JsonPatchDocument<ComentarioPatchDTO> patchDOC)
        {
            if (patchDOC is null)
            {
                return BadRequest();
            }

            var existeLibro = await context.Libros.AnyAsync(x => x.Id == libroId);
            if (!existeLibro)
            {
                return NotFound();
            }

            var usuario = await servicioUsuario.ObtenerUsuario();

            if (usuario == null) { return NotFound(); }

            var comentarioDB = await context.Comentarios.FirstOrDefaultAsync(x => x.Id == id);
            if (comentarioDB is null)
            {
                return NotFound();
            }

            if (comentarioDB.UsuarioID != usuario.Id)
            {
                return Forbid();
            }

            var comentarioPatchDTO = mapper.Map<ComentarioPatchDTO>(comentarioDB);
            patchDOC.ApplyTo(comentarioPatchDTO, ModelState);

            var esValido = TryValidateModel(comentarioPatchDTO);

            if (!esValido)
            {
                return ValidationProblem();
            }

            mapper.Map(comentarioPatchDTO, comentarioDB);
            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteComentario(Guid id, int libroID)
        {
            var existeLibro = await context.Libros.AnyAsync(x => x.Id == libroID);

            if (!existeLibro)
            {
                return NotFound();
            }

            var usuario = await servicioUsuario.ObtenerUsuario();

            if (usuario == null) { return NotFound(); }

            var comentarioDB = await context.Comentarios.FirstOrDefaultAsync(x => x.Id == id);

            if (comentarioDB is null)
            {
                return NotFound();
            }

            if (comentarioDB.UsuarioID != usuario.Id)
            {
                return Forbid();
            }

            context.Remove(comentarioDB);
            await context.SaveChangesAsync();

            return NoContent();
        }

    }
}
