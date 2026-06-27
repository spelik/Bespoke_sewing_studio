import { useState, type FormEvent } from "react";

type FormPayload = Record<string, FormDataEntryValue>;
type SubmitPrototype<TPayload> = (payload: TPayload) => void | Promise<unknown>;
type MapFormData<TPayload> = (formData: FormData) => TPayload;

const defaultMapFormData = (formData: FormData): FormPayload =>
  Object.fromEntries(formData);

const logPrototypeSubmit = (formName: string) => (payload: unknown) => {
  console.info(`[prototype] ${formName} submitted`, payload);
};

export function usePrototypeForm<TPayload = FormPayload>(
  formName: string,
  submit: SubmitPrototype<TPayload> = logPrototypeSubmit(formName),
  mapFormData: MapFormData<TPayload> = defaultMapFormData as MapFormData<TPayload>,
) {
  const [submitted, setSubmitted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);

    try {
      const payload = mapFormData(new FormData(event.currentTarget));
      await submit(payload);
      setSubmitted(true);
    } finally {
      setIsSubmitting(false);
    }
  };

  return { submitted, isSubmitting, handleSubmit };
}
