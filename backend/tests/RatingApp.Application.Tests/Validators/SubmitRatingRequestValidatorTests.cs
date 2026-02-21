using FluentAssertions;
using RatingApp.Application.DTOs.Ratings;
using RatingApp.Application.Validators;

namespace RatingApp.Application.Tests.Validators;

public class SubmitRatingRequestValidatorTests
{
    private readonly SubmitRatingRequestValidator _validator = new();

    private static SubmitRatingRequest ValidRequest() => new(
        RatedUserId: Guid.NewGuid(),
        Score: 7
    );

    [Fact]
    public void ValidRequest_IsValid()
    {
        var result = _validator.Validate(ValidRequest());
        result.IsValid.Should().BeTrue();
    }

    // ── RatedUserId ────────────────────────────────────────────────────────────

    [Fact]
    public void RatedUserId_Empty_Fails()
    {
        var req = ValidRequest() with { RatedUserId = Guid.Empty };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("required"));
    }

    // ── Score ──────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Score_ValidRange_Passes(int score)
    {
        var req = ValidRequest() with { Score = score };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public void Score_OutOfRange_Fails(int score)
    {
        var req = ValidRequest() with { Score = score };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Score must be between 1 and 10"));
    }
}
