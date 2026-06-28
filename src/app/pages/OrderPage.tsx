import { useEffect, useState, type DragEvent } from "react";
import { Check, ChevronDown, Send, Upload } from "lucide-react";
import {
  createOrder,
  getOrderSubmissionErrorMessage,
  validateOrderAttachments,
} from "../../api/ordersApi";
import { SectionLabel } from "../components/SectionLabel";
import { useAsyncForm } from "../hooks/useAsyncForm";
import type { OrderRequest, OrderSubmissionResponse } from "../types";
import { useServices } from "../services/ServicesContext";

export function OrderPage() {
  const { services } = useServices();
  const [service, setService] = useState("");
  const [consent, setConsent] = useState(false);
  const [attachments, setAttachments] = useState<File[]>([]);
  const [attachmentError, setAttachmentError] = useState<string | null>(null);
  const { submitted, result, isSubmitting, errorMessage, handleSubmit } = useAsyncForm<
    OrderRequest,
    OrderSubmissionResponse
  >(
    createOrder,
    (formData) => {
      const selectedValue = String(formData.get("service") ?? "");
      const selectedService = services.find(
        (item) => (item.id ?? `legacy:${item.slug}`) === selectedValue,
      );
      if (!selectedService) {
        throw new Error("Please select an available service.");
      }

      return {
        fullName: String(formData.get("fullName") ?? ""),
        email: String(formData.get("email") ?? ""),
        phone: String(formData.get("phone") ?? "") || undefined,
        serviceOfferingId: selectedService.id ?? undefined,
        serviceSlug:
          selectedService.id || selectedService.legacyServiceType
            ? undefined
            : selectedService.slug,
        legacyServiceType: selectedService.id
          ? undefined
          : selectedService.legacyServiceType,
        description: String(formData.get("description") ?? ""),
        preferredDate: String(formData.get("preferredDate") ?? "") || undefined,
        consent,
        attachments,
      };
    },
    getOrderSubmissionErrorMessage,
  );

  function selectAttachments(files: File[]) {
    try {
      validateOrderAttachments(files);
      setAttachments(files);
      setAttachmentError(null);
    } catch (error) {
      setAttachments([]);
      setAttachmentError(getOrderSubmissionErrorMessage(error));
    }
  }

  function handleDrop(event: DragEvent<HTMLLabelElement>) {
    event.preventDefault();
    selectAttachments(Array.from(event.dataTransfer.files));
  }

  useEffect(() => {
    if (submitted) {
      window.scrollTo({ top: 0, behavior: "auto" });
    }
  }, [submitted]);

  if (submitted) {
    return (
      <div className="pt-[72px] min-h-screen bg-background flex items-center justify-center px-6">
        <div className="text-center max-w-md">
          <div className="w-14 h-14 border border-accent/40 flex items-center justify-center mx-auto mb-6">
            <Check size={22} className="text-accent" />
          </div>
          <h2 className="font-serif text-[2rem] font-light text-foreground mb-4">
            Request Received
          </h2>
          <p className="text-[13px] text-muted-foreground leading-relaxed mb-8 font-sans">
            Thank you for your enquiry. We will be in touch within one working day to discuss your requirements and arrange a consultation at the studio.
          </p>
          <p className="text-[11px] text-muted-foreground/60 font-sans">
            Request reference: {result?.id}
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="pt-[72px]">
      <div className="bg-secondary py-20 px-6 lg:px-10">
        <div className="max-w-7xl mx-auto">
          <SectionLabel text="Order Request" />
          <h1 className="font-serif text-[3rem] lg:text-[4.5rem] font-light text-foreground mt-4 leading-tight">
            Begin Your
            <br />
            <em className="italic text-accent">Request.</em>
          </h1>
          <p className="text-[13px] text-muted-foreground mt-6 max-w-md leading-relaxed font-sans">
            Complete the form below and we will be in touch within one working day to discuss your requirements and arrange a consultation.
          </p>
        </div>
      </div>

      <div className="bg-background py-16">
        <div className="max-w-2xl mx-auto px-6 lg:px-10">
          <form
            className="space-y-10"
            onSubmit={handleSubmit}
            aria-busy={isSubmitting}
          >
            {/* Personal details */}
            <div>
              <h3 className="text-[10px] tracking-[0.3em] uppercase text-muted-foreground mb-6 pb-3 border-b border-border font-sans">
                Your Details
              </h3>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
                <div>
                  <label className="block text-[13px] text-foreground mb-2 font-sans font-medium">
                    Full Name <span className="text-accent">*</span>
                  </label>
                  <input
                    name="fullName"
                    type="text"
                    required
                    maxLength={200}
                    placeholder="Catherine O'Neill"
                    className="w-full border border-border bg-background px-4 py-3 text-[13px] focus:outline-none focus:border-accent transition-colors placeholder:text-muted-foreground/40 font-sans"
                  />
                </div>
                <div>
                  <label className="block text-[13px] text-foreground mb-2 font-sans font-medium">
                    Email Address <span className="text-accent">*</span>
                  </label>
                  <input
                    name="email"
                    type="email"
                    required
                    maxLength={320}
                    placeholder="catherine@example.com"
                    className="w-full border border-border bg-background px-4 py-3 text-[13px] focus:outline-none focus:border-accent transition-colors placeholder:text-muted-foreground/40 font-sans"
                  />
                </div>
                <div className="sm:col-span-2">
                  <label className="block text-[13px] text-foreground mb-2 font-sans font-medium">
                    Phone Number <span className="text-muted-foreground font-normal">(optional)</span>
                  </label>
                  <input
                    name="phone"
                    type="tel"
                    maxLength={50}
                    placeholder="+44 7700 900 000"
                    className="w-full border border-border bg-background px-4 py-3 text-[13px] focus:outline-none focus:border-accent transition-colors placeholder:text-muted-foreground/40 font-sans"
                  />
                </div>
              </div>
            </div>

            {/* Service details */}
            <div>
              <h3 className="text-[10px] tracking-[0.3em] uppercase text-muted-foreground mb-6 pb-3 border-b border-border font-sans">
                Service Details
              </h3>
              <div className="space-y-5">
                <div>
                  <label className="block text-[13px] text-foreground mb-2 font-sans font-medium">
                    Service Type <span className="text-accent">*</span>
                  </label>
                  <div className="relative">
                    <select
                      name="service"
                      value={service}
                      onChange={(e) => setService(e.target.value)}
                      required
                      className="w-full border border-border bg-background px-4 py-3 text-[13px] focus:outline-none focus:border-accent transition-colors appearance-none cursor-pointer font-sans"
                    >
                      <option value="">Select a service...</option>
                      {services.map((item) => (
                        <option
                          key={item.id ?? item.slug}
                          value={item.id ?? `legacy:${item.slug}`}
                        >
                          {item.name}
                        </option>
                      ))}
                    </select>
                    <ChevronDown
                      size={13}
                      className="absolute right-4 top-1/2 -translate-y-1/2 text-muted-foreground pointer-events-none"
                    />
                  </div>
                </div>

                <div>
                  <label className="block text-[13px] text-foreground mb-2 font-sans font-medium">
                    Description <span className="text-accent">*</span>
                  </label>
                  <textarea
                    name="description"
                    required
                    maxLength={4000}
                    rows={5}
                    placeholder="Please describe your garment and the work required. Include fabric, style, special requirements, and any relevant measurements..."
                    className="w-full border border-border bg-background px-4 py-3 text-[13px] focus:outline-none focus:border-accent transition-colors resize-none placeholder:text-muted-foreground/40 font-sans"
                  />
                </div>

                <div>
                  <label className="block text-[13px] text-foreground mb-2 font-sans font-medium">
                    Preferred Date <span className="text-muted-foreground font-normal">(for consultation)</span>
                  </label>
                  <input
                    name="preferredDate"
                    type="date"
                    className="w-full border border-border bg-background px-4 py-3 text-[13px] focus:outline-none focus:border-accent transition-colors text-muted-foreground font-sans"
                  />
                </div>

                {/* Photo upload */}
                <div>
                  <label className="block text-[13px] text-foreground mb-2 font-sans font-medium">
                    Photos of Your Garment{" "}
                    <span className="text-muted-foreground font-normal">(optional)</span>
                  </label>
                  <label
                    htmlFor="order-attachments"
                    onDragOver={(event) => event.preventDefault()}
                    onDrop={handleDrop}
                    className="border-2 border-dashed border-border hover:border-accent/50 transition-colors p-10 text-center cursor-pointer bg-secondary/20 group"
                  >
                    <input
                      id="order-attachments"
                      type="file"
                      multiple
                      accept=".jpg,.jpeg,.png,.webp,.pdf,image/jpeg,image/png,image/webp,application/pdf"
                      className="sr-only"
                      onChange={(event) => selectAttachments(Array.from(event.target.files ?? []))}
                    />
                    <Upload size={22} className="mx-auto mb-3 text-muted-foreground/40 group-hover:text-accent/60 transition-colors" />
                    <p className="text-[13px] text-muted-foreground font-sans">
                      Drag and drop images here, or{" "}
                      <span className="text-accent font-medium">click to browse</span>
                    </p>
                    <p className="text-[11px] text-muted-foreground/50 mt-2 font-sans">
                      JPG, PNG, WebP or PDF &middot; Up to 5 MB each &middot; Maximum 5 files
                    </p>
                  </label>
                  {attachments.length > 0 ? (
                    <ul className="mt-3 space-y-1.5" aria-label="Selected attachments">
                      {attachments.map((file) => (
                        <li key={`${file.name}-${file.size}`} className="text-[11px] text-muted-foreground font-sans">
                          {file.name} &middot; {(file.size / 1024).toFixed(1)} KB
                        </li>
                      ))}
                    </ul>
                  ) : null}
                  {attachmentError ? (
                    <p role="alert" className="text-[11px] text-accent mt-3 font-sans">
                      {attachmentError}
                    </p>
                  ) : null}
                </div>
              </div>
            </div>

            {/* Consent */}
            <div className="pt-1">
              <label className="flex items-start gap-3 cursor-pointer group">
                <div
                  role="checkbox"
                  aria-checked={consent}
                  tabIndex={0}
                  onClick={() => setConsent((current) => !current)}
                  onKeyDown={(event) => {
                    if (event.key === " " || event.key === "Enter") {
                      event.preventDefault();
                      setConsent((current) => !current);
                    }
                  }}
                  className={`w-4 h-4 mt-0.5 border shrink-0 flex items-center justify-center transition-colors cursor-pointer ${
                    consent ? "bg-foreground border-foreground" : "border-border group-hover:border-foreground"
                  }`}
                >
                  {consent && <Check size={10} className="text-primary-foreground" />}
                </div>
                <span className="text-[13px] text-muted-foreground leading-relaxed font-sans">
                  I consent to Bespoke Sewing Studio storing my contact information and enquiry details in order to process my request. I have read and agree to the{" "}
                  <span className="text-foreground underline underline-offset-2 cursor-pointer">Privacy Policy</span>.
                </span>
              </label>
            </div>

            {/* Submit */}
            {errorMessage ? (
              <p
                role="alert"
                className="text-[12px] text-accent text-center font-sans leading-relaxed"
              >
                {errorMessage}
              </p>
            ) : null}

            <button
              type="submit"
              disabled={!consent || isSubmitting || attachmentError !== null}
              className="w-full bg-foreground text-primary-foreground py-4 text-[13px] tracking-wide hover:bg-accent transition-colors flex items-center justify-center gap-2.5 font-sans disabled:cursor-not-allowed disabled:opacity-50 disabled:hover:bg-foreground"
            >
              <Send size={13} />
              {isSubmitting ? "Submitting..." : "Submit Order Request"}
            </button>

            <p className="text-[11px] text-muted-foreground/50 text-center font-sans">
              Your request will be sent securely to Bespoke Sewing Studio.
            </p>
          </form>
        </div>
      </div>
    </div>
  );
}

