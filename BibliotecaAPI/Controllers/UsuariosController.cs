using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BibliotecaAPI.Controllers
{
    [ApiController]
    [Route("api/usuarios")]
    public class UsuariosController: ControllerBase
    {
        private readonly UserManager<Usuario> userManager;
        private readonly IConfiguration configuration;
        private readonly SignInManager<Usuario> signInManager;
        private readonly IServicioUsuarios servicioUsuarios;
        private readonly ApplicationDbContext context;
        private readonly IMapper mapper;

        public UsuariosController(UserManager<Usuario> userManager, IConfiguration configuration,
            SignInManager<Usuario> signInManager, IServicioUsuarios servicioUsuarios, ApplicationDbContext context, IMapper mapper)
        {
            this.userManager = userManager;
            this.configuration = configuration;
            this.signInManager = signInManager;
            this.servicioUsuarios = servicioUsuarios;
            this.context = context;
            this.mapper = mapper;
        }

        [HttpGet]
        [Authorize(Policy = "esadmin")]
        public async Task<IEnumerable<UsuarioDTO>> SeleccionarUsuarios()
        {
            var usuarios = await context.Users.ToListAsync();
            var usuariosDTO = mapper.Map<IEnumerable<UsuarioDTO>>(usuarios);
            return usuariosDTO;
        }

        [HttpPost("registro")]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Registrar(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var usuario = new Usuario
            {
                UserName = credencialesUsuarioDTO.Email,
                Email = credencialesUsuarioDTO.Email
            };

            var resultado = await userManager.CreateAsync(usuario, credencialesUsuarioDTO.Password!);

            if (resultado.Succeeded)
            {
                var respuestaAutenticacion = await ConstruirToken(credencialesUsuarioDTO);
                return respuestaAutenticacion;
            }
            else 
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return ValidationProblem();
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> Login(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var usuario = await userManager.FindByEmailAsync(credencialesUsuarioDTO.Email);

            if (usuario is null)
            {
                return RetornarLoginIncorrecto();
            }

            var resultado = await signInManager.CheckPasswordSignInAsync(usuario, credencialesUsuarioDTO.Password!, lockoutOnFailure: false);

            if (resultado.Succeeded)
            {
                return await ConstruirToken(credencialesUsuarioDTO);
            }
            else 
            { 
                return RetornarLoginIncorrecto(); 
            }
        }

        [HttpPut]
        [Authorize]
        public async Task<ActionResult> ActualziarUsuario(ActualizarUsuarioDTO actualizarUsuarioDTO)
        {
            var usuario = await servicioUsuarios.ObtenerUsuario();

            if (usuario is null)
            {
                return NotFound();
            }

            usuario.FechaNacimiento = actualizarUsuarioDTO.FechaNacimiento;
            await userManager.UpdateAsync(usuario);

            return NoContent();

        }

        [HttpGet("renovar-token")]
        [Authorize]
        public async Task<ActionResult<RespuestaAutenticacionDTO>> RenovarToken()
        {
            var usuario = await servicioUsuarios.ObtenerUsuario();

            if (usuario is null)
            {
                return NotFound();
            }

            var credencialesUsuarioDTO = new CredencialesUsuarioDTO { Email = usuario.Email! };

            var respuestaAutenticacion = await ConstruirToken(credencialesUsuarioDTO);
            return respuestaAutenticacion;
        }

        [HttpPost("hacer-admin")]
        [Authorize(Policy = "esadmin")]
        public async Task<ActionResult> HacerAdmin(EditarClaimDTO editarClaimDTO)
        {
            var usuario = await userManager.FindByEmailAsync(editarClaimDTO.Email);

            if (usuario is null)
            {
                return NotFound();
            }

            await userManager.AddClaimAsync(usuario, new Claim("esadmin", "true"));
            return NoContent();
        }


        [HttpPost("remover-admin")]
        [Authorize(Policy = "esadmin")]
        public async Task<ActionResult> RemoverAdmin(EditarClaimDTO editarClaimDTO)
        {
            var usuario = await userManager.FindByEmailAsync(editarClaimDTO.Email);

            if (usuario is null)
            {
                return NotFound();
            }

            await userManager.RemoveClaimAsync(usuario, new Claim("esadmin", "true"));
            return NoContent();
        }



        private ActionResult RetornarLoginIncorrecto()
        {
            ModelState.AddModelError(string.Empty, "Login incorrecto");
            return ValidationProblem();
        }

        private async Task<RespuestaAutenticacionDTO> ConstruirToken(CredencialesUsuarioDTO credencialesUsuarioDTO)
        {
            var claims = new List<Claim>
            {
                new Claim("email", credencialesUsuarioDTO.Email),
                new Claim("lo que yo quiera", "Cualquier Valor")
            };

            var usuario = await userManager.FindByEmailAsync(credencialesUsuarioDTO.Email);
            var claimsDB = await userManager.GetClaimsAsync(usuario!);

            claims.AddRange(claimsDB);

            var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["llavejwt"]!));
            var credenciales = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256);

            var expiracion = DateTime.UtcNow.AddYears(1);

            var tokenDeSeguridad = new JwtSecurityToken(issuer: null, audience: null,
                claims: claims, expires: expiracion, signingCredentials: credenciales);

            var token = new JwtSecurityTokenHandler().WriteToken(tokenDeSeguridad);

            return new RespuestaAutenticacionDTO
            {
                Token = token,
                Expiracion = expiracion
            };
        }
    }
}
