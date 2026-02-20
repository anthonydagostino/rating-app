using Microsoft.AspNetCore.Components.Forms;
using RatingApp.Client.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace RatingApp.Client.Services;

public class PhotoApiService
{
    private readonly HttpClient _http;

    public PhotoApiService(HttpClient http) => _http = http;

    public Task<List<PhotoDto>?> GetPhotosAsync() =>
        _http.GetFromJsonAsync<List<PhotoDto>>("api/photos");

    public async Task<PhotoDto?> UploadPhotoAsync(IBrowserFile file)
    {
        using var content = new MultipartFormDataContent();
        var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(streamContent, "file", file.Name);

        var response = await _http.PostAsync("api/photos", content);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PhotoDto>();
    }

    public async Task<bool> DeletePhotoAsync(Guid photoId)
    {
        var response = await _http.DeleteAsync($"api/photos/{photoId}");
        return response.IsSuccessStatusCode;
    }
}
