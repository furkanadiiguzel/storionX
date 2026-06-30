import { Routes, Route } from "react-router-dom";
import { Moon, Sun, Server } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { useTheme } from "@/hooks/use-theme";

function DarkModeToggle() {
  const { theme, setTheme } = useTheme();
  return (
    <Button
      variant="ghost"
      size="icon"
      onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
      aria-label="Toggle dark mode"
    >
      {theme === "dark" ? <Sun className="size-4" /> : <Moon className="size-4" />}
    </Button>
  );
}

function Sidebar() {
  return (
    <aside className="flex h-screen w-56 flex-col border-r bg-card px-3 py-4">
      <div className="mb-6 flex items-center gap-2 px-2">
        <Server className="size-5 text-primary" />
        <span className="font-semibold tracking-tight">storionX</span>
        <Badge variant="secondary" className="ml-auto text-xs">
          Migration
        </Badge>
      </div>

      <nav className="flex flex-col gap-1">
        <Button variant="ghost" className="justify-start">
          Runs
        </Button>
        <Button variant="ghost" className="justify-start">
          Archives
        </Button>
        <Button variant="ghost" className="justify-start">
          Audit Log
        </Button>
        <Button variant="ghost" className="justify-start">
          Reconciliation
        </Button>
      </nav>

      <div className="mt-auto flex items-center justify-between px-2">
        <span className="text-xs text-muted-foreground">v1.0.0</span>
        <DarkModeToggle />
      </div>
    </aside>
  );
}

function HomePage() {
  return (
    <div className="p-8">
      <h1 className="text-2xl font-bold">Migration Dashboard</h1>
      <p className="mt-2 text-muted-foreground">
        EV → storionX migration pipeline monitor.
      </p>
      <Separator className="my-6" />
      <div className="flex gap-3">
        <Button>Start Run</Button>
        <Button variant="outline">View Audit</Button>
        <Button variant="secondary">Reconcile</Button>
      </div>
    </div>
  );
}

export default function App() {
  return (
    <div className="flex min-h-screen bg-background text-foreground">
      <Sidebar />
      <main className="flex-1 overflow-auto">
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route
            path="*"
            element={
              <div className="p-8 text-muted-foreground">Page not found</div>
            }
          />
        </Routes>
      </main>
    </div>
  );
}
