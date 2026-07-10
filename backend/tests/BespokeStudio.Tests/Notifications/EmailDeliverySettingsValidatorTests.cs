using BespokeStudio.Application.Contracts.Notifications;
using BespokeStudio.Application.Validation;

namespace BespokeStudio.Tests.Notifications;

public sealed class EmailDeliverySettingsValidatorTests
{
    [Fact]
    public void Validate_AcceptsCompleteResendApiSettings()
    {
        var errors = EmailDeliverySettingsValidator.Validate(new UpdateEmailDeliverySettingsRequest(
            Provider: "ResendApi",
            GmailAddress: null,
            SenderName: "Bespoke Sewing Studio",
            AppPassword: null,
            ClearAppPassword: false,
            ResendFromEmail: "noreply@oksanalogosha.com",
            ReplyToEmail: "contact@oksanalogosha.com",
            ResendApiKey: "re_test_key_value",
            ClearResendApiKey: false));

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_RequiresResendFromAndReplyToEmails()
    {
        var errors = EmailDeliverySettingsValidator.Validate(new UpdateEmailDeliverySettingsRequest(
            Provider: "ResendApi",
            GmailAddress: null,
            SenderName: "Bespoke Sewing Studio",
            AppPassword: null,
            ClearAppPassword: false,
            ResendFromEmail: "not-an-email",
            ReplyToEmail: null,
            ResendApiKey: "re_test_key_value",
            ClearResendApiKey: false));

        Assert.Contains(nameof(UpdateEmailDeliverySettingsRequest.ResendFromEmail), errors.Keys);
        Assert.Contains(nameof(UpdateEmailDeliverySettingsRequest.ReplyToEmail), errors.Keys);
    }
}
