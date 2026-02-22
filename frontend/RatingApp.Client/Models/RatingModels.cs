namespace RatingApp.Client.Models;

public record SimpleRatingRequest(Guid RatedUserId, int Score, string? Comment = null);

public record RatingSummaryModel(double AverageScore, int RatingCount, double Percentile, string TopPercentLabel);
