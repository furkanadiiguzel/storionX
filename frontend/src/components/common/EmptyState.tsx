import { type LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";

interface EmptyStateProps {
  icon?: LucideIcon;
  title: string;
  description?: string;
  action?: React.ReactNode;
  className?: string;
}

export function EmptyState({ icon: Icon, title, description, action, className }: EmptyStateProps) {
  return (
    <div className={cn("flex flex-col items-center gap-3 py-12 text-center", className)}>
      {Icon && <Icon className="size-10 text-muted-foreground/50" />}
      <p className="font-medium text-muted-foreground">{title}</p>
      {description && <p className="text-sm text-muted-foreground/70">{description}</p>}
      {action}
    </div>
  );
}
