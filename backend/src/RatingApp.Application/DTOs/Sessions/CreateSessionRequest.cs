namespace RatingApp.Application.DTOs.Sessions;

public record CreateSessionRequest(Guid CandidateId, string? Title = null);
