"use client";

import { AppFormModal } from "@/components/AppFormModal";
import { SupplierCreateForm } from "./SupplierCreateForm";

export function SupplierCreateModal() {
  return (
    <AppFormModal title="Create Supplier" description="Add a supplier master record." buttonLabel="+ New Supplier">
      {({ close }) => <SupplierCreateForm close={close} />}
    </AppFormModal>
  );
}
