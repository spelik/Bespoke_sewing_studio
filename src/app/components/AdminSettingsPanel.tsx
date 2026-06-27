import { useEffect, useState, type FormEvent } from "react";
import { ApiError } from "../../api/apiClient";
import {
  getAdminSiteSettings,
  updateAdminSiteSettings,
} from "../../api/siteSettingsApi";
import type {
  AdminSiteSettings,
  UpdateSiteSettingsRequest,
} from "../types";
import { useSiteSettings } from "../siteSettings/SiteSettingsContext";

interface AdminSettingsPanelProps {
  onUnauthorized(): void;
}

type TextFieldName = Exclude<
  keyof UpdateSiteSettingsRequest,
  "emailNotificationsEnabled" | "whatsAppNotificationsEnabled"
>;

const inputClassName =
  "w-full border border-border bg-background px-3 py-2.5 text-[12px] text-foreground focus:outline-none focus:border-accent transition-colors font-sans";

export function AdminSettingsPanel({ onUnauthorized }: AdminSettingsPanelProps) {
  const { refresh } = useSiteSettings();
  const [form, setForm] = useState<UpdateSiteSettingsRequest | null>(null);
  const [updatedAt, setUpdatedAt] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    getAdminSiteSettings()
      .then((settings) => {
        if (active) {
          setForm(toUpdateRequest(settings));
          setUpdatedAt(settings.updatedAt);
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

  const setTextField = (field: TextFieldName, value: string) => {
    setForm((current) =>
      current ? { ...current, [field]: value || null } : current,
    );
    setSuccess(null);
  };

  const setBooleanField = (
    field: "emailNotificationsEnabled" | "whatsAppNotificationsEnabled",
    value: boolean,
  ) => {
    setForm((current) => (current ? { ...current, [field]: value } : current));
    setSuccess(null);
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

  if (isLoading) {
    return (
      <div className="bg-card border border-border p-6 text-[12px] text-muted-foreground font-sans">
        Loading settings...
      </div>
    );
  }

  if (!form) {
    return (
      <div role="alert" className="bg-card border border-destructive/30 p-6 text-[12px] text-destructive font-sans">
        {error ?? "Settings could not be loaded."}
      </div>
    );
  }

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
          label="Phone / WhatsApp number"
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
          Notifications will be sent to the email and phone shown above. Development providers currently log messages without sending them externally.
        </p>
        <SettingsCheckbox
          label="Enable email notifications"
          checked={form.emailNotificationsEnabled}
          onChange={(checked) => setBooleanField("emailNotificationsEnabled", checked)}
        />
        <SettingsCheckbox
          label="Enable WhatsApp notifications"
          checked={form.whatsAppNotificationsEnabled}
          onChange={(checked) => setBooleanField("whatsAppNotificationsEnabled", checked)}
        />
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
}: {
  label: string;
  value: string | null;
  onChange(value: string): void;
  type?: "text" | "email" | "tel" | "url";
  required?: boolean;
}) {
  return (
    <label className="text-[11px] text-muted-foreground font-sans">
      <span className="block mb-1.5">{label}</span>
      <input
        type={type}
        required={required}
        value={value ?? ""}
        onChange={(event) => onChange(event.target.value)}
        className={inputClassName}
      />
    </label>
  );
}

function SettingsTextArea({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string | null;
  onChange(value: string): void;
}) {
  return (
    <label className="text-[11px] text-muted-foreground font-sans md:col-span-2">
      <span className="block mb-1.5">{label}</span>
      <textarea
        rows={3}
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
    whatsAppNotificationsEnabled: settings.whatsAppNotificationsEnabled,
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
