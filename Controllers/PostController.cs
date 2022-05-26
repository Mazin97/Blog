using Blog.Data;
using Blog.Models;
using Blog.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Blog.Controllers;

[ApiController]
[Route("v1/posts")]
public class PostController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index(
        [FromServices] BlogDataContext context,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 25
    )
    {
        try
        {
            var posts = await context
                .Posts
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.Author)
                .Skip(page * pageSize)
                .Take(pageSize)
                .OrderByDescending(x => x.LastUpdateDate)
                .ToListAsync();

            return Ok(new ResultViewModel<List<Post>>(posts));
        }
        catch
        {
            return StatusCode(500, new ResultViewModel<string>("5x02 - Falha interna do sistema"));
        }
    }
    
    [HttpGet("/{id:int}")]
    public async Task<IActionResult> Index(
        [FromServices] BlogDataContext context,
        [FromRoute] int id
    )
    {
        try
        {
            var post = await context
                .Posts
                .AsNoTracking()
                .Include(x => x.Category)
                .Include(x => x.Author)
                    .ThenInclude(x => x.Roles)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (post == null)
                return NotFound(new ResultViewModel<string>("5x02 - Post não encontrado"));
            
            return Ok(new ResultViewModel<Post>(post));
        }
        catch
        {
            return StatusCode(500, new ResultViewModel<string>("5x02 - Falha interna do sistema"));
        }
    }
}