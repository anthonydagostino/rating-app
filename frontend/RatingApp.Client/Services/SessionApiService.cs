using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using RatingApp.Client.Models;

namespace RatingApp.Client.Services;

/// <summary>
/// Combines REST calls (create/get session) with a SignalR hub connection
/// for real-time collaborative rating sessions.
/// </summary>
public class SessionApiService : IAsyncDisposable
{
    private readonly HttpClient _http;
    private HubConnection? _hub;

    // Events raised when the hub pushes updates
    public event Action<ParticipantRatingDto>? OnRatingSubmitted;
    public event Action<ParticipantRatingDto>? OnRatingUpdated;
    public event Action<SessionChatMessageDto>? OnChatMessage;
    public event Action<Guid>? OnUserJoined;
    public event Action<Guid>? OnUserLeft;

    public bool IsConnected => _hub?.State == HubConnectionState.Connected;

    public SessionApiService(HttpClient http) => _http = http;

    // --- REST ---

    public async Task<SessionDto?> CreateSessionAsync(CreateSessionRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/sessions", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<SessionDto>()
            : null;
    }

    public Task<SessionDto?> GetSessionAsync(Guid sessionId) =>
        _http.GetFromJsonAsync<SessionDto>($"api/sessions/{sessionId}");

    public async Task LockSessionAsync(Guid sessionId) =>
        await _http.PostAsync($"api/sessions/{sessionId}/lock", null);

    public async Task FinalizeSessionAsync(Guid sessionId) =>
        await _http.PostAsync($"api/sessions/{sessionId}/finalize", null);

    // --- SignalR ---

    public async Task ConnectAsync(string hubUrl, string accessToken)
    {
        _hub = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        _hub.On<ParticipantRatingDto>("RatingSubmitted", dto => OnRatingSubmitted?.Invoke(dto));
        _hub.On<ParticipantRatingDto>("RatingUpdated", dto => OnRatingUpdated?.Invoke(dto));
        _hub.On<SessionChatMessageDto>("ChatMessage", dto => OnChatMessage?.Invoke(dto));
        _hub.On<object>("UserJoined", obj =>
        {
            // obj is anonymous { userId, sessionId }
        });
        _hub.On<object>("UserLeft", obj =>
        {
            // obj is anonymous { userId, sessionId }
        });

        await _hub.StartAsync();
    }

    public async Task JoinSessionAsync(Guid sessionId)
    {
        EnsureConnected();
        await _hub!.InvokeAsync("JoinSession", sessionId);
    }

    public async Task LeaveSessionAsync(Guid sessionId)
    {
        if (_hub?.State == HubConnectionState.Connected)
            await _hub.InvokeAsync("LeaveSession", sessionId);
    }

    public async Task SubmitRatingAsync(Guid sessionId, SubmitSessionRatingRequest request)
    {
        EnsureConnected();
        await _hub!.InvokeAsync("SubmitRating", sessionId, request);
    }

    public async Task UpdateRatingAsync(Guid sessionId, UpdateSessionRatingRequest request)
    {
        EnsureConnected();
        await _hub!.InvokeAsync("UpdateRating", sessionId, request);
    }

    public async Task SendChatMessageAsync(Guid sessionId, string content)
    {
        EnsureConnected();
        await _hub!.InvokeAsync("SendChatMessage", sessionId, content);
    }

    public async Task<SessionStateDto?> GetSessionStateAsync(Guid sessionId)
    {
        EnsureConnected();
        return await _hub!.InvokeAsync<SessionStateDto>("SessionState", sessionId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null)
            await _hub.DisposeAsync();
    }

    private void EnsureConnected()
    {
        if (_hub?.State != HubConnectionState.Connected)
            throw new InvalidOperationException("Hub connection is not established.");
    }
}