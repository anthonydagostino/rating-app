using FluentValidation;
using RatingApp.Application.DTOs.Ratings;

namespace RatingApp.Application.Validators;

public class SubmitRatingRequestValidator : AbstractValidator<SubmitRatingRequest>
{
    public SubmitRatingRequestValidator()
    {
        RuleFor(x => x.RatedUserId)
            .NotEmpty().WithMessage("RatedUserId is required.");

        RuleFor(x => x.Score)
            .InclusiveBetween(1, 10).WithMessage("Score must be between 1 and 10.");
    }
}
