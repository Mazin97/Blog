using System.Text.RegularExpressions;
using Blog.Data;
using Blog.Extensions;
using Blog.Models;
using Blog.Services;
using Blog.ViewModels;
using Blog.ViewModels.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureIdentity.Password;

namespace Blog.Controllers;

[Authorize]
[ApiController]
[Route("v1/accounts/")]
public class AccountController : ControllerBase
{
    private readonly TokenService _tokenService;
    
    public AccountController(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> PostAsync(
        [FromBody] RegisterViewModel model,
        [FromServices] EmailService emailService,
        [FromServices] BlogDataContext context
    )
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResultViewModel<string>(ModelState.GetErrors()));

        var user = new User()
        {
            Name = model.Name,
            Email = model.Email,
            Slug = model.Email.Replace("@", "-").Replace(".", "-")
        };

        var password = PasswordGenerator.Generate(25);
        user.PasswordHash = PasswordHasher.Hash(password);

        try
        {
            await context.AddAsync(user);
            await context.SaveChangesAsync();

            emailService.Send(
                user.Name,
                user.Email, 
                "Bem vindo ao Blog!",
                $"Sua senha é <strong>{password}</strong>");
            
            return Ok(new ResultViewModel<dynamic>(new
            {
                user = user.Email,
                password
            }));
        }
        catch (DbUpdateException)
        {
            return StatusCode(500, new ResultViewModel<string>("05X99 - E-mail já cadastrado"));
        }
        catch
        {
            return StatusCode(500, new ResultViewModel<string>("05X04 - Falha interna do sistema"));
        }
    }
    
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginViewModel model,
        [FromServices] BlogDataContext context
        )
    {
        if (!ModelState.IsValid)
            return BadRequest(new ResultViewModel<string>(ModelState.GetErrors()));

        try
        {
            var user = await context
                .Users
                .AsNoTracking()
                .Include(x => x.Roles)
                .FirstOrDefaultAsync(x => x.Email == model.Email);

            if (user == null || !PasswordHasher.Verify(user.PasswordHash, model.Password))
                return BadRequest(new ResultViewModel<string>("Usuário ou senha inválido"));
            
            return Ok(new ResultViewModel<string>(_tokenService.GenerateToken(user), null));
        }
        catch (Exception)
        {
            return StatusCode(500, new ResultViewModel<string>("05X04 - Falha interna do sistema"));
        }
    }

    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage(
        [FromBody] UploadImageViewModel model,
        [FromServices] BlogDataContext context)
    {
        var fileName = $"{Guid.NewGuid().ToString()}.jpg";
        var data = new Regex(@"^data:image\/[a-z]+;base64,").Replace(model.Base64Image, ""); 
        var bytes = Convert.FromBase64String(data);

        try
        {
            await System.IO.File.WriteAllBytesAsync($"wwwroot/images/{fileName}", bytes);
        }
        catch
        {
            return StatusCode(500, new ResultViewModel<string>("05x4 - Falha interna do sistema"));
        }

        var user = await context
            .Users
            .FirstOrDefaultAsync((x => x.Email == User.Identity.Name));
        if (user == null) return NotFound(new ResultViewModel<string>("Usuário não encontrado"));

        user.Image = $"https://localhost:0000/images/{fileName}";
        context.Users.Update(user);
        await context.SaveChangesAsync();

        return Ok(new ResultViewModel<string>("Imagem alterada com sucesso", null));
    }
}