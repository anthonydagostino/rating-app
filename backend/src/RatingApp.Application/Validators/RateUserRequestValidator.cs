using FluentValidation;
using RatingApp.Application.DTOs.Ratings;

namespace RatingApp.Application.Validators;

public class RateUserRequestValidator : AbstractValidator<RateUserRequest>
{
    public RateUserRequestValidator()
    {
        RuleFor(x => x.RatedUserId)
            .NotEmpty().WithMessage("RatedUserId is required.");

        RuleFor(x => x.Score)
            .InclusiveBetween(1, 10).WithMessage("Score must be between 1 and 10.");

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("Comment must not exceed 500 characters.")
            .When(x => x.Comment is not null);
    }
}
