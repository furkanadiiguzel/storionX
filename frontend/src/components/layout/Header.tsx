import { Moon, Sun, Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useTheme } from "@/hooks/use-theme";

interface Props { onNewRun: () => void; }

export function Header({ onNewRun }: Props) {
  const { theme, setTheme } = useTheme();

  return (
    <header className="sticky top-0 z-20 flex h-14 items-center border-b bg-card/80 backdrop-blur px-4 gap-2">
      <span className="mr-auto text-sm font-medium text-muted-foreground md:hidden">
        storionX Migration
      </span>
      <Button size="sm" onClick={onNewRun} aria-label="Start a new migration run">
        <Plus className="size-4" />
        New Run
      </Button>
      <Button
        variant="ghost"
        size="icon"
        onClick={() => setTheme(theme === "dark" ? "light" : "dark")}
        aria-label="Toggle dark mode"
      >
        {theme === "dark" ? <Sun className="size-4" /> : <Moon className="size-4" />}
      </Button>
    </header>
  );
}
