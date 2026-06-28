import type { ReactNode } from "react";
import { Link } from "react-router-dom";

export function AppLink({ href, className, children }: { href: string; className?: string; children: ReactNode }) {
  return href.startsWith("/") && !href.startsWith("//")
    ? <Link to={href} className={className}>{children}</Link>
    : <a href={href} className={className}>{children}</a>;
}
