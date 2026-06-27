import { Scissors } from "lucide-react";

export function StitchDivider() {
  return (
    <div className="flex items-center gap-2 py-2">
      {Array.from({ length: 12 }).map((_, index) => (
        <div key={index} className="h-px flex-1 border-t border-dashed border-border/40" />
      ))}
      <Scissors size={12} className="text-accent/40 shrink-0" />
      {Array.from({ length: 12 }).map((_, index) => (
        <div key={index} className="h-px flex-1 border-t border-dashed border-border/40" />
      ))}
    </div>
  );
}
