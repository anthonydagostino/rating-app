namespace RatingApp.Client.Models;

public record CandidateDto(
    Guid UserId,
    string DisplayName,
    int Age,
    int Gender,
    double DistanceMiles,
    IReadOnlyList<string>? PhotoUrls
);

public record SubmitRatingRequest(Guid RatedUserId, int Score);

public record RatingResult(bool MatchCreated, Guid? MatchId);
