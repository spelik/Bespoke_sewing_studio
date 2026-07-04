using BespokeStudio.Application.Contracts.Orders;
using BespokeStudio.Application.Validation;
using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Tests.Validation;

public sealed class OrderRequestValidatorTests
{
    [Fact]
    public void Validate_ValidMinimalRequest_ReturnsNoErrors()
    {
        var request = CreateValidRequest();

        var errors = OrderRequestValidator.Validate(request);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_FilledHoneypot_ReturnsFormError()
    {
        var request = CreateValidRequest() with { WebsiteUrl = "https://spam.example" };

        var errors = OrderRequestValidator.Validate(request);

        Assert.Contains("Form", errors.Keys);
    }

    [Fact]
    public void Validate_InvalidEmail_ReturnsEmailError()
    {
        var request = CreateValidRequest() with { Email = "not-an-email" };

        var errors = OrderRequestValidator.Validate(request);

        Assert.Contains(nameof(CreateOrderRequest.Email), errors.Keys);
    }

    [Fact]
    public void Validate_MissingConsent_ReturnsConsentError()
    {
        var request = CreateValidRequest() with { Consent = false };

        var errors = OrderRequestValidator.Validate(request);

        Assert.Contains(nameof(CreateOrderRequest.Consent), errors.Keys);
    }

    [Fact]
    public void Validate_MoreThanFiveAttachments_ReturnsAttachmentError()
    {
        var request = CreateValidRequest() with
        {
            AttachmentIds = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToArray()
        };

        var errors = OrderRequestValidator.Validate(request);

        Assert.Contains(nameof(CreateOrderRequest.AttachmentIds), errors.Keys);
    }

    private static CreateOrderRequest CreateValidRequest() => new(
        FullName: "Test Customer",
        Email: "customer@example.com",
        Phone: null,
        ServiceType: OrderServiceType.Alterations,
        ServiceOfferingId: null,
        ServiceSlug: null,
        Description: "Shorten a pair of trousers.",
        PreferredDate: null,
        Consent: true,
        AttachmentIds: null,
        WebsiteUrl: null,
        FormLoadedAt: DateTimeOffset.UtcNow.AddMinutes(-1));
}
