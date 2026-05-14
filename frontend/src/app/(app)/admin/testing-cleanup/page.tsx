import { Card } from "@/components/ui";
import { TestDataCleanupPanel } from "./TestDataCleanupPanel";

export default function TestingCleanupPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">Testing Cleanup</h1>
        <p className="mt-1 text-sm text-zinc-500">Admin-only cleanup tools for resetting test transaction data.</p>
      </div>

      <Card>
        <TestDataCleanupPanel />
      </Card>
    </div>
  );
}
