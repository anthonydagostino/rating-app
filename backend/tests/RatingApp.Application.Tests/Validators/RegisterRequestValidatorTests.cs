using FluentAssertions;
using RatingApp.Application.DTOs.Auth;
using RatingApp.Application.Validators;

namespace RatingApp.Application.Tests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    private static RegisterRequest ValidRequest() => new(
        Email: "user@example.com",
        Password: "Password1",
        DisplayName: "Alice",
        Gender: 1,
        Birthdate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20)),
        Latitude: 40.7128,
        Longitude: -74.0060
    );

    // ── Email ──────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidRequest_IsValid()
    {
        var result = _validator.Validate(ValidRequest());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Email_Empty_FailsWithRequiredMessage()
    {
        var req = ValidRequest() with { Email = "" };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Email_InvalidFormat_FailsWithFormatMessage()
    {
        var req = ValidRequest() with { Email = "not-an-email" };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Invalid email"));
    }

    [Fact]
    public void Email_TooLong_Fails()
    {
        var longEmail = new string('a', 251) + "@x.com"; // 257 chars > MaximumLength(254)
        var req = ValidRequest() with { Email = longEmail };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
    }

    // ── Password ───────────────────────────────────────────────────────────────

    [Fact]
    public void Password_Empty_Fails()
    {
        var req = ValidRequest() with { Password = "" };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Password_TooShort_Fails()
    {
        var req = ValidRequest() with { Password = "Ab1" };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("8 characters"));
    }

    [Fact]
    public void Password_NoUppercase_Fails()
    {
        var req = ValidRequest() with { Password = "password1" };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("uppercase"));
    }

    [Fact]
    public void Password_NoDigit_Fails()
    {
        var req = ValidRequest() with { Password = "Password" };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("digit"));
    }

    // ── DisplayName ────────────────────────────────────────────────────────────

    [Fact]
    public void DisplayName_Empty_Fails()
    {
        var req = ValidRequest() with { DisplayName = "" };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DisplayName");
    }

    [Fact]
    public void DisplayName_TooLong_Fails()
    {
        var req = ValidRequest() with { DisplayName = new string('a', 101) };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
    }

    // ── Gender ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Gender_ValidValues_Pass(int gender)
    {
        var req = ValidRequest() with { Gender = gender };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(-1)]
    public void Gender_InvalidValues_Fail(int gender)
    {
        var req = ValidRequest() with { Gender = gender };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Gender must be 1"));
    }

    // ── Birthdate ──────────────────────────────────────────────────────────────

    [Fact]
    public void Birthdate_ExactlyAge18_IsValid()
    {
        var req = ValidRequest() with
        {
            Birthdate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18))
        };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Birthdate_Under18_Fails()
    {
        var req = ValidRequest() with
        {
            Birthdate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-17))
        };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("18 years"));
    }

    // ── Latitude / Longitude ───────────────────────────────────────────────────

    [Theory]
    [InlineData(-90.0)]
    [InlineData(0.0)]
    [InlineData(90.0)]
    public void Latitude_BoundaryValues_AreValid(double lat)
    {
        var req = ValidRequest() with { Latitude = lat };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-90.1)]
    [InlineData(90.1)]
    public void Latitude_OutOfRange_Fails(double lat)
    {
        var req = ValidRequest() with { Latitude = lat };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Latitude"));
    }

    [Theory]
    [InlineData(-180.0)]
    [InlineData(0.0)]
    [InlineData(180.0)]
    public void Longitude_BoundaryValues_AreValid(double lon)
    {
        var req = ValidRequest() with { Longitude = lon };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(-180.1)]
    [InlineData(180.1)]
    public void Longitude_OutOfRange_Fails(double lon)
    {
        var req = ValidRequest() with { Longitude = lon };
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Longitude"));
    }
}
