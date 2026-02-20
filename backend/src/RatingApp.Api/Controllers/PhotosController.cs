using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RatingApp.Application.Services;

namespace RatingApp.Api.Controllers;

[ApiController]
[Route("api/photos")]
[Authorize]
public class PhotosController : ControllerBase
{
    private readonly PhotoService _photoService;

    public PhotosController(PhotoService photoService) => _photoService = photoService;

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User ID claim not found."));

    [HttpGet]
    public async Task<IActionResult> GetPhotos() =>
        Ok(await _photoService.GetUserPhotosAsync(CurrentUserId));

    [HttpPost]
    public async Task<IActionResult> UploadPhoto(IFormFile file)
    {
        try
        {
            var dto = await _photoService.UploadPhotoAsync(CurrentUserId, file);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{photoId:guid}")]
    public async Task<IActionResult> DeletePhoto(Guid photoId)
    {
        try
        {
            await _photoService.DeletePhotoAsync(CurrentUserId, photoId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
