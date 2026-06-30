import { NavLink } from "react-router-dom";
import { LayoutDashboard, Play, Archive, Server } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

const NAV = [
  { to: "/",         label: "Dashboard", icon: LayoutDashboard },
  { to: "/runs",     label: "Runs",      icon: Play },
  { to: "/archives", label: "Archives",  icon: Archive },
];

export function AppSidebar() {
  return (
    <aside className="hidden md:flex h-screen w-56 flex-col border-r bg-card px-3 py-4 shrink-0">
      <div className="mb-6 flex items-center gap-2 px-2">
        <Server className="size-5 text-primary" />
        <span className="font-semibold tracking-tight">storionX</span>
        <Badge variant="secondary" className="ml-auto text-xs">Migration</Badge>
      </div>

      <nav className="flex flex-col gap-1">
        {NAV.map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            end={to === "/"}
            className={({ isActive }) =>
              cn(
                "flex items-center gap-2 rounded-md px-3 py-2 text-sm transition-colors",
                isActive
                  ? "bg-primary text-primary-foreground font-medium"
                  : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
              )
            }
          >
            <Icon className="size-4 shrink-0" aria-hidden="true" />
            {label}
          </NavLink>
        ))}
      </nav>
    </aside>
  );
}
