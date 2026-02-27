export type ReferenceFormRoute = {
  code: string;
  routeTemplate?: string | null;
  isActive: boolean;
};

export function buildReferenceRouteMap(forms: ReferenceFormRoute[]): Map<string, string> {
  return new Map(
    forms
      .filter((form) => form.isActive && !!form.routeTemplate)
      .map((form) => [form.code.toUpperCase(), form.routeTemplate as string]),
  );
}

export function resolveReferenceHref(
  routeMap: Map<string, string>,
  referenceType: string,
  referenceId: string,
): string | null {
  const template = routeMap.get(referenceType.toUpperCase());
  if (!template) {
    return null;
  }

  return template.replace("{id}", referenceId);
}
