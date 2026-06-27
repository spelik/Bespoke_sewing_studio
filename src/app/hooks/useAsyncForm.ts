import { useState, type FormEvent } from "react";

type SubmitForm<TPayload, TResult> = (payload: TPayload) => Promise<TResult>;
type MapFormData<TPayload> = (formData: FormData) => TPayload;
type GetErrorMessage = (error: unknown) => string;

export function useAsyncForm<TPayload, TResult>(
  submit: SubmitForm<TPayload, TResult>,
  mapFormData: MapFormData<TPayload>,
  getErrorMessage: GetErrorMessage,
) {
  const [result, setResult] = useState<TResult | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);
    setErrorMessage(null);

    try {
      const payload = mapFormData(new FormData(event.currentTarget));
      setResult(await submit(payload));
    } catch (error) {
      setErrorMessage(getErrorMessage(error));
    } finally {
      setIsSubmitting(false);
    }
  };

  return {
    submitted: result !== null,
    result,
    isSubmitting,
    errorMessage,
    handleSubmit,
  };
}
