export function SectionLabel({ text }: { text: string }) {
  return (
    <div className="flex items-center gap-4 mb-4">
      <div className="h-px flex-1 bg-border max-w-[40px]" />
      <span className="text-[10px] tracking-[0.35em] uppercase text-muted-foreground font-sans">
        {text}
      </span>
      <div className="h-px w-8 bg-border" />
    </div>
  );
}
