using System.Net.Http.Json;
using RatingApp.Client.Models;

namespace RatingApp.Client.Services;

public class ChatApiService
{
    private readonly HttpClient _http;

    public ChatApiService(HttpClient http) => _http = http;

    public Task<List<ChatSummaryDto>?> GetChatsAsync() =>
        _http.GetFromJsonAsync<List<ChatSummaryDto>>("api/chats");

    public Task<List<MessageDto>?> GetMessagesAsync(Guid chatId) =>
        _http.GetFromJsonAsync<List<MessageDto>>($"api/chats/{chatId}/messages");

    public async Task<MessageDto?> SendMessageAsync(Guid chatId, string content)
    {
        var response = await _http.PostAsJsonAsync(
            $"api/chats/{chatId}/messages", new SendMessageRequest(content));
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<MessageDto>()
            : null;
    }
}
