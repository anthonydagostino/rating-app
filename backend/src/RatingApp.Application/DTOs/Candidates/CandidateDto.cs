namespace RatingApp.Application.DTOs.Candidates;

public record CandidateDto(
    Guid UserId,
    string DisplayName,
    int Age,
    int Gender,
    double DistanceMiles,
    IReadOnlyList<string> PhotoUrls
);
