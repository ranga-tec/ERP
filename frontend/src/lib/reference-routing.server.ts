import { cache } from "react";
import { backendFetchJson } from "@/lib/backend.server";
import {
  buildReferenceRouteMap,
  resolveReferenceHref,
  type ReferenceFormRoute,
} from "@/lib/reference-routing";

export const getReferenceRouteMap = cache(async () => {
  const forms = await backendFetchJson<ReferenceFormRoute[]>("/reference-forms");
  return buildReferenceRouteMap(forms);
});

export async function getReferenceHref(
  referenceType: string,
  referenceId?: string | null,
): Promise<string | null> {
  if (!referenceId) {
    return null;
  }

  const routeMap = await getReferenceRouteMap();
  return resolveReferenceHref(routeMap, referenceType, referenceId);
}
