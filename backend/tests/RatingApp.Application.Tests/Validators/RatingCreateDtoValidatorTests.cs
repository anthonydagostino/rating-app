using FluentAssertions;
using RatingApp.Application.DTOs.Ratings;
using RatingApp.Application.Validators;

namespace RatingApp.Application.Tests.Validators;

public class RatingCreateDtoValidatorTests
{
    private readonly RatingCreateDtoValidator _validator = new();

    private static readonly Guid SkillId = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid CommId  = new("00000000-0000-0000-0000-000000000002");

    private static RatingCreateDto ValidDto() => new(
        RatedUserId: Guid.NewGuid(),
        Criteria: new List<RatingCriterionScoreDto>
        {
            new(SkillId, 8),
            new(CommId,  7)
        },
        Comment: null
    );

    // ── Happy path ──────────────────────────────────────────────────────────

    [Fact]
    public void ValidDto_IsValid()
    {
        var result = _validator.Validate(ValidDto());
        result.IsValid.Should().BeTrue();
    }

    // ── RatedUserId ─────────────────────────────────────────────────────────

    [Fact]
    public void RatedUserId_Empty_Fails()
    {
        var dto = ValidDto() with { RatedUserId = Guid.Empty };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("RatedUserId is required"));
    }

    // ── Criteria list ────────────────────────────────────────────────────────

    [Fact]
    public void Criteria_Null_Fails()
    {
        var dto = new RatingCreateDto(Guid.NewGuid(), null!, null);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Criteria_Empty_Fails()
    {
        var dto = ValidDto() with { Criteria = new List<RatingCriterionScoreDto>() };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("At least one criterion score must be provided"));
    }

    // ── Score range ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    [InlineData(100)]
    public void Score_OutOfRange_Fails(int score)
    {
        var dto = new RatingCreateDto(
            Guid.NewGuid(),
            new List<RatingCriterionScoreDto> { new(SkillId, score) },
            null);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Score must be between 1 and 10"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Score_BoundaryValues_IsValid(int score)
    {
        var dto = new RatingCreateDto(
            Guid.NewGuid(),
            new List<RatingCriterionScoreDto> { new(SkillId, score) },
            null);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    // ── CriterionId ──────────────────────────────────────────────────────────

    [Fact]
    public void CriterionId_Empty_Fails()
    {
        var dto = new RatingCreateDto(
            Guid.NewGuid(),
            new List<RatingCriterionScoreDto> { new(Guid.Empty, 8) },
            null);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("CriterionId is required"));
    }

    // ── Duplicate criteria ───────────────────────────────────────────────────

    [Fact]
    public void Criteria_DuplicateCriterionIds_Fails()
    {
        var dupId = Guid.NewGuid();
        var dto = new RatingCreateDto(
            Guid.NewGuid(),
            new List<RatingCriterionScoreDto>
            {
                new(dupId, 8),
                new(dupId, 7)
            },
            null);
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Duplicate criteria are not allowed"));
    }

    // ── Comment ──────────────────────────────────────────────────────────────

    [Fact]
    public void Comment_Null_IsValid()
    {
        var dto = ValidDto() with { Comment = null };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Comment_Empty_IsValid()
    {
        var dto = ValidDto() with { Comment = "" };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Comment_AtMaxLength_IsValid()
    {
        var dto = ValidDto() with { Comment = new string('x', 500) };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Comment_ExceedsMaxLength_Fails()
    {
        var dto = ValidDto() with { Comment = new string('x', 501) };
        var result = _validator.Validate(dto);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Comment must not exceed 500 characters"));
    }
}