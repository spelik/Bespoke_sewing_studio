import { useEffect, useState, type FormEvent } from "react";
import { ApiError } from "../../api/apiClient";
import {
  getAdminEmailDeliverySettings,
  getAdminSiteSettings,
  sendTestEmailNotification,
  updateAdminEmailDeliverySettings,
  updateAdminSiteSettings,
} from "../../api/siteSettingsApi";
import type {
  AdminEmailDeliverySettings,
  AdminSiteSettings,
  EmailDeliveryProvider,
  UpdateEmailDeliverySettingsRequest,
  UpdateSiteSettingsRequest,
} from "../types";
import { useSiteSettings } from "../siteSettings/SiteSettingsContext";

interface AdminSettingsPanelProps {
  onUnauthorized(): void;
}

type TextFieldName = Exclude<
  keyof UpdateSiteSettingsRequest,
  "emailNotificationsEnabled" | "customerConfirmationEmailsEnabled"
>;

type EmailDeliveryTextFieldName = Exclude<
  keyof UpdateEmailDeliverySettingsRequest,
  "provider" | "clearAppPassword"
>;

const inputClassName =
  "w-full border border-border bg-background px-3 py-2.5 text-[12px] text-foreground focus:outline-none focus:border-accent transition-colors font-sans";

export function AdminSettingsPanel({ onUnauthorized }: AdminSettingsPanelProps) {
  const { refresh } = useSiteSettings();
  const [form, setForm] = useState<UpdateSiteSettingsRequest | null>(null);
  const [emailDelivery, setEmailDelivery] =
    useState<UpdateEmailDeliverySettingsRequest | null>(null);
  const [emailDeliveryPasswordConfigured, setEmailDeliveryPasswordConfigured] =
    useState(false);
  const [updatedAt, setUpdatedAt] = useState<string | null>(null);
  const [emailDeliveryUpdatedAt, setEmailDeliveryUpdatedAt] = useState<
    string | null
  >(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isSavingEmailDelivery, setIsSavingEmailDelivery] = useState(false);
  const [isTestingEmail, setIsTestingEmail] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [emailDeliveryError, setEmailDeliveryError] = useState<string | null>(
    null,
  );
  const [success, setSuccess] = useState<string | null>(null);
  const [emailDeliverySuccess, setEmailDeliverySuccess] = useState<
    string | null
  >(null);
  const [testEmailResult, setTestEmailResult] = useState<{
    success: boolean;
    message: string;
  } | null>(null);

  useEffect(() => {
    let active = true;

    Promise.all([getAdminSiteSettings(), getAdminEmailDeliverySettings()])
      .then(([settings, deliverySettings]) => {
        if (active) {
          setForm(toUpdateRequest(settings));
          setUpdatedAt(settings.updatedAt);
          applyEmailDeliverySettings(deliverySettings);
        }
      })
      .catch((reason: unknown) => {
        if (!active) {
          return;
        }

        if (reason instanceof ApiError && reason.status === 401) {
          onUnauthorized();
          return;
        }

        setError(getErrorMessage(reason));
      })
      .finally(() => {
        if (active) {
          setIsLoading(false);
        }
      });

    return () => {
      active = false;
    };
  }, [onUnauthorized]);

  const applyEmailDeliverySettings = (
    settings: AdminEmailDeliverySettings,
  ) => {
    setEmailDelivery({
      provider: settings.provider,
      gmailAddress: settings.gmailAddress,
      senderName: settings.senderName,
      appPassword: null,
      clearAppPassword: false,
    });
    setEmailDeliveryPasswordConfigured(settings.appPasswordConfigured);
    setEmailDeliveryUpdatedAt(settings.updatedAt);
  };

  const setTextField = (field: TextFieldName, value: string) => {
    setForm((current) =>
      current ? { ...current, [field]: value || null } : current,
    );
    setSuccess(null);
  };

  const setEmailDeliveryProvider = (provider: EmailDeliveryProvider) => {
    setEmailDelivery((current) =>
      current ? { ...current, provider, clearAppPassword: false } : current,
    );
    setEmailDeliverySuccess(null);
    setEmailDeliveryError(null);
    setTestEmailResult(null);
  };

  const setEmailDeliveryTextField = (
    field: EmailDeliveryTextFieldName,
    value: string,
  ) => {
    setEmailDelivery((current) =>
      current ? { ...current, [field]: value || null } : current,
    );
    setEmailDeliverySuccess(null);
    setEmailDeliveryError(null);
    setTestEmailResult(null);
  };

  const setClearAppPassword = (value: boolean) => {
    setEmailDelivery((current) =>
      current
        ? {
            ...current,
            clearAppPassword: value,
            appPassword: value ? null : current.appPassword,
          }
        : current,
    );
    setEmailDeliverySuccess(null);
    setEmailDeliveryError(null);
    setTestEmailResult(null);
  };

  const setEmailNotificationsEnabled = (value: boolean) => {
    setForm((current) =>
      current ? { ...current, emailNotificationsEnabled: value } : current,
    );
    setSuccess(null);
    setTestEmailResult(null);
  };

  const setCustomerConfirmationEmailsEnabled = (value: boolean) => {
    setForm((current) =>
      current
        ? { ...current, customerConfirmationEmailsEnabled: value }
        : current,
    );
    setSuccess(null);
  };

  const handleTestEmail = async () => {
    if (isTestingEmail) {
      return;
    }

    setIsTestingEmail(true);
    setTestEmailResult(null);

    try {
      const result = await sendTestEmailNotification();
      setTestEmailResult({
        success: result.success,
        message: `${result.message} Provider: ${result.provider}.`,
      });
    } catch (reason: unknown) {
      if (reason instanceof ApiError && reason.status === 401) {
        onUnauthorized();
        return;
      }

      setTestEmailResult({ success: false, message: getErrorMessage(reason) });
    } finally {
      setIsTestingEmail(false);
    }
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!form || isSaving) {
      return;
    }

    setIsSaving(true);
    setError(null);
    setSuccess(null);

    try {
      const saved = await updateAdminSiteSettings(form);
      setForm(toUpdateRequest(saved));
      setUpdatedAt(saved.updatedAt);
      await refresh().catch(() => undefined);
      setSuccess("Settings saved successfully.");
    } catch (reason: unknown) {
      if (reason instanceof ApiError && reason.status === 401) {
        onUnauthorized();
        return;
      }

      setError(getErrorMessage(reason));
    } finally {
      setIsSaving(false);
    }
  };

  const handleSaveEmailDelivery = async () => {
    if (!emailDelivery || isSavingEmailDelivery) {
      return;
    }

    setIsSavingEmailDelivery(true);
    setEmailDeliveryError(null);
    setEmailDeliverySuccess(null);

    try {
      const saved = await updateAdminEmailDeliverySettings(emailDelivery);
      applyEmailDeliverySettings(saved);
      setEmailDeliverySuccess("Email delivery settings saved successfully.");
    } catch (reason: unknown) {
      if (reason instanceof ApiError && reason.status === 401) {
        onUnauthorized();
        return;
      }

      setEmailDeliveryError(getErrorMessage(reason));
    } finally {
      setIsSavingEmailDelivery(false);
    }
  };

  if (isLoading) {
    return (
      <div className="bg-card border border-border p-6 text-[12px] text-muted-foreground font-sans">
        Loading settings...
      </div>
    );
  }

  if (!form || !emailDelivery) {
    return (
      <div role="alert" className="bg-card border border-destructive/30 p-6 text-[12px] text-destructive font-sans">
        {error ?? "Settings could not be loaded."}
      </div>
    );
  }

  const isGmailSmtp = emailDelivery.provider === "GmailSmtp";

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {error ? (
        <div role="alert" className="border border-destructive/30 bg-card px-4 py-3 text-[11px] text-destructive font-sans">
          {error}
        </div>
      ) : null}
      {success ? (
        <div role="status" className="border border-emerald-300 bg-emerald-50 px-4 py-3 text-[11px] text-emerald-700 font-sans">
          {success}
        </div>
      ) : null}

      <SettingsGroup title="General">
        <SettingsField
          label="Studio name"
          required
          value={form.studioName}
          onChange={(value) => setTextField("studioName", value)}
        />
        <SettingsTextArea
          label="Site tagline / short description"
          value={form.siteTagline}
          onChange={(value) => setTextField("siteTagline", value)}
        />
        <SettingsField
          label="Business legal name"
          value={form.businessLegalName}
          onChange={(value) => setTextField("businessLegalName", value)}
        />
      </SettingsGroup>

      <SettingsGroup title="Contact">
        <SettingsField
          label="Email"
          type="email"
          value={form.email}
          onChange={(value) => setTextField("email", value)}
        />
        <SettingsField
          label="Phone"
          type="tel"
          value={form.phone}
          onChange={(value) => setTextField("phone", value)}
        />
        <SettingsField
          label="Contact button label"
          value={form.contactButtonLabel}
          onChange={(value) => setTextField("contactButtonLabel", value)}
        />
        <SettingsTextArea
          label="Contact intro text"
          value={form.contactIntroText}
          onChange={(value) => setTextField("contactIntroText", value)}
        />
        <SettingsField
          label="Service area text"
          value={form.serviceAreaText}
          onChange={(value) => setTextField("serviceAreaText", value)}
        />
        <SettingsTextArea
          label="Footer text"
          value={form.footerText}
          onChange={(value) => setTextField("footerText", value)}
        />
      </SettingsGroup>

      <SettingsGroup title="Notifications">
        <p className="md:col-span-2 text-[11px] text-muted-foreground font-sans leading-relaxed">
          Owner notifications are sent to the contact email above when a new contact message or order request arrives. Save contact settings before sending a test email.
        </p>
        <SettingsCheckbox
          label="Notify me about new requests"
          checked={form.emailNotificationsEnabled}
          onChange={setEmailNotificationsEnabled}
        />
        <button
          type="button"
          disabled={isTestingEmail}
          onClick={handleTestEmail}
          className="border border-border bg-background px-4 py-3 text-[11px] text-foreground hover:border-accent disabled:opacity-50 transition-colors font-sans"
        >
          {isTestingEmail ? "Sending test..." : "Send test email"}
        </button>
        {testEmailResult ? (
          <p
            role={testEmailResult.success ? "status" : "alert"}
            className={`md:col-span-2 text-[11px] font-sans ${
              testEmailResult.success ? "text-emerald-700" : "text-destructive"
            }`}
          >
            {testEmailResult.message}
          </p>
        ) : null}
      </SettingsGroup>

      <SettingsGroup title="Customer confirmations">
        <p className="md:col-span-2 text-[11px] text-muted-foreground font-sans leading-relaxed">
          Send a short automatic confirmation to the email address entered by the customer. These templates are plain-text emails.
        </p>
        <SettingsCheckbox
          label="Send automatic confirmation to customers"
          checked={form.customerConfirmationEmailsEnabled}
          onChange={setCustomerConfirmationEmailsEnabled}
        />
        <div className="md:col-span-2 border border-border bg-background p-4 text-[11px] text-muted-foreground font-sans leading-relaxed space-y-2">
          <p className="text-foreground">Available placeholders</p>
          <p>{"{{studioName}}"}, {"{{customerName}}"}, {"{{customerEmail}}"}, {"{{customerPhone}}"}</p>
          <p>Order only: {"{{serviceName}}"}, {"{{preferredDate}}"}</p>
          <p>Contact only: {"{{messageSubject}}"}</p>
        </div>
        <SettingsField
          label="Order confirmation subject"
          value={form.customerOrderConfirmationSubject}
          onChange={(value) => setTextField("customerOrderConfirmationSubject", value)}
        />
        <SettingsTextArea
          label="Order confirmation body"
          value={form.customerOrderConfirmationBody}
          rows={7}
          onChange={(value) => setTextField("customerOrderConfirmationBody", value)}
        />
        <SettingsField
          label="Contact confirmation subject"
          value={form.customerContactConfirmationSubject}
          onChange={(value) => setTextField("customerContactConfirmationSubject", value)}
        />
        <SettingsTextArea
          label="Contact confirmation body"
          value={form.customerContactConfirmationBody}
          rows={7}
          onChange={(value) => setTextField("customerContactConfirmationBody", value)}
        />
      </SettingsGroup>

      <SettingsGroup title="Email delivery">
        {emailDeliveryError ? (
          <div role="alert" className="md:col-span-2 border border-destructive/30 bg-background px-4 py-3 text-[11px] text-destructive font-sans">
            {emailDeliveryError}
          </div>
        ) : null}
        {emailDeliverySuccess ? (
          <div role="status" className="md:col-span-2 border border-emerald-300 bg-emerald-50 px-4 py-3 text-[11px] text-emerald-700 font-sans">
            {emailDeliverySuccess}
          </div>
        ) : null}
        <p className="md:col-span-2 text-[11px] text-muted-foreground font-sans leading-relaxed">
          Choose Configuration for developer-managed SMTP from server settings, or Gmail SMTP when the site owner manages the sender Gmail account here.
        </p>
        <SettingsSelect
          label="Delivery provider"
          value={emailDelivery.provider}
          onChange={setEmailDeliveryProvider}
        />
        <SettingsField
          label="Sender name"
          value={emailDelivery.senderName}
          onChange={(value) => setEmailDeliveryTextField("senderName", value)}
        />
        <SettingsField
          label="Gmail address"
          type="email"
          disabled={!isGmailSmtp}
          value={emailDelivery.gmailAddress}
          onChange={(value) => setEmailDeliveryTextField("gmailAddress", value)}
        />
        <SettingsField
          label={
            emailDeliveryPasswordConfigured
              ? "New Google App Password (leave blank to keep current)"
              : "Google App Password"
          }
          type="password"
          disabled={!isGmailSmtp || emailDelivery.clearAppPassword}
          value={emailDelivery.appPassword}
          onChange={(value) => setEmailDeliveryTextField("appPassword", value)}
        />
        <p className="text-[11px] text-muted-foreground font-sans leading-relaxed">
          Password status: {emailDeliveryPasswordConfigured ? "configured" : "not configured"}. The saved password is encrypted on the backend and is never shown here.
        </p>
        <SettingsCheckbox
          label="Clear saved App Password"
          checked={emailDelivery.clearAppPassword}
          onChange={setClearAppPassword}
        />
        <div className="md:col-span-2 border border-border bg-background p-4 text-[11px] text-muted-foreground font-sans leading-relaxed space-y-2">
          <p className="text-foreground">Gmail App Password guide</p>
          <p>1. Open your Google Account, then Security.</p>
          <p>2. Turn on 2-Step Verification.</p>
          <p>3. Open App Passwords and create a new password for this website.</p>
          <p>4. Paste the 16-character App Password here. Do not use the normal Gmail password.</p>
        </div>
        <div className="md:col-span-2 flex flex-wrap items-center justify-between gap-4">
          <p className="text-[10px] text-muted-foreground font-sans">
            {emailDeliveryUpdatedAt
              ? `Email delivery updated ${new Date(emailDeliveryUpdatedAt).toLocaleString()}`
              : "Email delivery settings not saved yet"}
          </p>
          <button
            type="button"
            disabled={isSavingEmailDelivery}
            onClick={handleSaveEmailDelivery}
            className="border border-border bg-background px-4 py-3 text-[11px] text-foreground hover:border-accent disabled:opacity-50 transition-colors font-sans"
          >
            {isSavingEmailDelivery ? "Saving email delivery..." : "Save Email Delivery"}
          </button>
        </div>
      </SettingsGroup>

      <SettingsGroup title="Social links">
        <SettingsField label="Facebook URL" type="url" value={form.facebookUrl} onChange={(value) => setTextField("facebookUrl", value)} />
        <SettingsField label="Instagram URL" type="url" value={form.instagramUrl} onChange={(value) => setTextField("instagramUrl", value)} />
        <SettingsField label="TikTok URL" type="url" value={form.tikTokUrl} onChange={(value) => setTextField("tikTokUrl", value)} />
        <SettingsField label="Pinterest URL" type="url" value={form.pinterestUrl} onChange={(value) => setTextField("pinterestUrl", value)} />
      </SettingsGroup>

      <div className="flex flex-wrap items-center justify-between gap-4 bg-card border border-border p-5">
        <p className="text-[10px] text-muted-foreground font-sans">
          {updatedAt ? `Last updated ${new Date(updatedAt).toLocaleString()}` : "Not saved yet"}
        </p>
        <button
          type="submit"
          disabled={isSaving}
          className="bg-foreground text-primary-foreground px-6 py-2.5 text-[11px] tracking-wide hover:bg-accent disabled:opacity-50 transition-colors font-sans"
        >
          {isSaving ? "Saving..." : "Save Settings"}
        </button>
      </div>
    </form>
  );
}

function SettingsGroup({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="bg-card border border-border p-5 lg:p-6">
      <h2 className="text-[12px] font-medium text-foreground mb-5 font-sans tracking-wide">{title}</h2>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">{children}</div>
    </section>
  );
}

function SettingsField({
  label,
  value,
  onChange,
  type = "text",
  required = false,
  disabled = false,
}: {
  label: string;
  value: string | null;
  onChange(value: string): void;
  type?: "text" | "email" | "tel" | "url" | "password";
  required?: boolean;
  disabled?: boolean;
}) {
  return (
    <label className="text-[11px] text-muted-foreground font-sans">
      <span className="block mb-1.5">{label}</span>
      <input
        type={type}
        required={required}
        disabled={disabled}
        value={value ?? ""}
        onChange={(event) => onChange(event.target.value)}
        className={`${inputClassName} disabled:opacity-60`}
      />
    </label>
  );
}

function SettingsSelect({
  label,
  value,
  onChange,
}: {
  label: string;
  value: EmailDeliveryProvider;
  onChange(value: EmailDeliveryProvider): void;
}) {
  return (
    <label className="text-[11px] text-muted-foreground font-sans">
      <span className="block mb-1.5">{label}</span>
      <select
        value={value}
        onChange={(event) => onChange(event.target.value as EmailDeliveryProvider)}
        className={inputClassName}
      >
        <option value="Configuration">Configuration / server secrets</option>
        <option value="GmailSmtp">Gmail SMTP</option>
      </select>
    </label>
  );
}

function SettingsTextArea({
  label,
  value,
  onChange,
  rows = 3,
}: {
  label: string;
  value: string | null;
  onChange(value: string): void;
  rows?: number;
}) {
  return (
    <label className="text-[11px] text-muted-foreground font-sans md:col-span-2">
      <span className="block mb-1.5">{label}</span>
      <textarea
        rows={rows}
        value={value ?? ""}
        onChange={(event) => onChange(event.target.value)}
        className={`${inputClassName} resize-y`}
      />
    </label>
  );
}

function SettingsCheckbox({
  label,
  checked,
  onChange,
}: {
  label: string;
  checked: boolean;
  onChange(checked: boolean): void;
}) {
  return (
    <label className="flex items-center gap-3 border border-border bg-background px-3 py-3 text-[11px] text-foreground font-sans">
      <input
        type="checkbox"
        checked={checked}
        onChange={(event) => onChange(event.target.checked)}
        className="accent-foreground"
      />
      {label}
    </label>
  );
}

function toUpdateRequest(settings: AdminSiteSettings): UpdateSiteSettingsRequest {
  return {
    studioName: settings.studioName,
    siteTagline: settings.siteTagline,
    email: settings.email,
    phone: settings.phone,
    contactButtonLabel: settings.contactButtonLabel,
    contactIntroText: settings.contactIntroText,
    emailNotificationsEnabled: settings.emailNotificationsEnabled,
    customerConfirmationEmailsEnabled: settings.customerConfirmationEmailsEnabled,
    customerOrderConfirmationSubject: settings.customerOrderConfirmationSubject,
    customerOrderConfirmationBody: settings.customerOrderConfirmationBody,
    customerContactConfirmationSubject: settings.customerContactConfirmationSubject,
    customerContactConfirmationBody: settings.customerContactConfirmationBody,
    facebookUrl: settings.facebookUrl,
    instagramUrl: settings.instagramUrl,
    tikTokUrl: settings.tikTokUrl,
    pinterestUrl: settings.pinterestUrl,
    footerText: settings.footerText,
    serviceAreaText: settings.serviceAreaText,
    businessLegalName: settings.businessLegalName,
  };
}

function getErrorMessage(reason: unknown): string {
  if (reason instanceof ApiError) {
    const validationMessages = reason.errors
      ? Object.values(reason.errors).flat()
      : [];
    if (validationMessages.length > 0) {
      return validationMessages.join(" ");
    }

    if (reason.status === 403) {
      return "Administrator authorization is required to edit settings.";
    }

    return reason.message;
  }

  return "Settings could not be saved. Please try again.";
}
