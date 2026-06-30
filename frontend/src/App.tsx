import { useState, lazy, Suspense } from "react";
import { Routes, Route } from "react-router-dom";
import { AppSidebar } from "@/components/layout/AppSidebar";
import { Header } from "@/components/layout/Header";
import { NewRunSheet } from "@/pages/NewRun";
import { ErrorBoundary } from "@/components/common/ErrorBoundary";
import { DetailSkeleton } from "@/components/common/LoadingSkeleton";

const Dashboard = lazy(() => import("@/pages/Dashboard"));
const Runs       = lazy(() => import("@/pages/Runs"));
const RunDetail  = lazy(() => import("@/pages/RunDetail"));
const Archives   = lazy(() => import("@/pages/Archives"));

export default function App() {
  const [newRunOpen, setNewRunOpen] = useState(false);

  return (
    <div className="flex min-h-screen bg-background text-foreground">
      <AppSidebar />
      <div className="flex flex-1 flex-col min-w-0">
        <Header onNewRun={() => setNewRunOpen(true)} />
        <main className="flex-1 overflow-auto">
          <ErrorBoundary>
            <Suspense fallback={<DetailSkeleton />}>
              <Routes>
                <Route path="/"         element={<Dashboard />} />
                <Route path="/runs"     element={<Runs />} />
                <Route path="/runs/:id" element={<RunDetail />} />
                <Route path="/archives" element={<Archives />} />
                <Route path="*"         element={<p className="p-8 text-muted-foreground">Page not found</p>} />
              </Routes>
            </Suspense>
          </ErrorBoundary>
        </main>
      </div>
      <NewRunSheet open={newRunOpen} onOpenChange={setNewRunOpen} />
    </div>
  );
}
