namespace RatingApp.Application.DTOs.Ratings;

public record CriterionDto(Guid Id, string Name, double Weight, bool IsRequired);
