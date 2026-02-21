using FluentValidation;
using RatingApp.Application.DTOs.Ratings;

namespace RatingApp.Application.Validators;

public class RatingCreateDtoValidator : AbstractValidator<RatingCreateDto>
{
    public RatingCreateDtoValidator()
    {
        RuleFor(x => x.RatedUserId)
            .NotEmpty().WithMessage("RatedUserId is required.");

        RuleFor(x => x.Criteria)
            .NotNull().WithMessage("Criteria list is required.")
            .NotEmpty().WithMessage("At least one criterion score must be provided.");

        RuleForEach(x => x.Criteria).ChildRules(criterion =>
        {
            criterion.RuleFor(c => c.CriterionId)
                .NotEmpty().WithMessage("CriterionId is required.");

            criterion.RuleFor(c => c.Score)
                .InclusiveBetween(1, 10).WithMessage("Score must be between 1 and 10.");
        });

        RuleFor(x => x.Criteria)
            .Must(criteria => criteria == null || criteria.Select(c => c.CriterionId).Distinct().Count() == criteria.Count)
            .WithMessage("Duplicate criteria are not allowed.");

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("Comment must not exceed 500 characters.")
            .When(x => x.Comment is not null);
    }
}