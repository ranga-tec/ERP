"use client";

import {
  Children,
  Fragment,
  isValidElement,
  useEffect,
  useId,
  useMemo,
  useRef,
  useState,
  type ChangeEvent,
  type ComponentProps,
  type FocusEvent,
  type KeyboardEvent,
  type ReactNode,
} from "react";

type SearchableSelectProps = Omit<ComponentProps<"select">, "multiple" | "size">;

type ParsedOption = {
  key: string;
  value: string;
  label: string;
  disabled: boolean;
  searchText: string;
};

type ChildNodeProps = {
  children?: ReactNode;
};

type OptionNodeProps = ChildNodeProps & {
  value?: string | number | readonly string[];
  disabled?: boolean;
};

type OptGroupNodeProps = ChildNodeProps & {
  label?: string;
};

const inputClassName =
  "w-full rounded-md border border-[var(--input-border)] bg-[var(--surface)] px-2.5 py-1.5 text-[13px] text-[var(--foreground)] shadow-[var(--shadow-control)] outline-none transition focus-visible:border-[var(--link)] focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] placeholder:text-[var(--muted-foreground)]/80";

function cx(...parts: Array<string | null | undefined | false>) {
  return parts.filter(Boolean).join(" ");
}

function nodeText(node: ReactNode): string {
  if (typeof node === "string" || typeof node === "number") {
    return String(node);
  }

  if (Array.isArray(node)) {
    return node.map((child) => nodeText(child)).join(" ").trim();
  }

  if (isValidElement<ChildNodeProps>(node)) {
    return nodeText(node.props.children);
  }

  return "";
}

function normalizeValue(value: SearchableSelectProps["value"] | SearchableSelectProps["defaultValue"]): string {
  if (Array.isArray(value)) {
    return value.length > 0 ? String(value[0] ?? "") : "";
  }

  if (value === undefined || value === null) {
    return "";
  }

  return String(value);
}

function parseOptions(children: ReactNode, groupLabel?: string, items: ParsedOption[] = []): ParsedOption[] {
  Children.forEach(children, (child, index) => {
    if (!isValidElement<OptionNodeProps | OptGroupNodeProps | ChildNodeProps>(child)) {
      return;
    }

    if (child.type === Fragment) {
      parseOptions((child.props as ChildNodeProps).children, groupLabel, items);
      return;
    }

    if (child.type === "optgroup") {
      const optGroupProps = child.props as OptGroupNodeProps;
      const nextGroupLabel = optGroupProps.label ? String(optGroupProps.label) : groupLabel;
      parseOptions(optGroupProps.children, nextGroupLabel, items);
      return;
    }

    if (child.type !== "option") {
      return;
    }

    const optionProps = child.props as OptionNodeProps;
    const optionValue = optionProps.value !== undefined ? String(optionProps.value) : nodeText(optionProps.children);
    const label = nodeText(optionProps.children) || optionValue;
    items.push({
      key: child.key !== null ? String(child.key) : `${groupLabel ?? "option"}-${optionValue}-${index}`,
      value: optionValue,
      label,
      disabled: Boolean(optionProps.disabled),
      searchText: [label, groupLabel ?? ""].join(" ").toLowerCase(),
    });
  });

  return items;
}

function createSelectChangeEvent(value: string, name?: string): ChangeEvent<HTMLSelectElement> {
  const target = { value, name: name ?? "" } as EventTarget & HTMLSelectElement;
  return {
    target,
    currentTarget: target,
  } as ChangeEvent<HTMLSelectElement>;
}

export function Select({
  className,
  children,
  value: valueProp,
  defaultValue,
  onChange,
  onBlur,
  onFocus,
  onKeyDown,
  name,
  required,
  disabled,
  form,
  id,
  autoFocus,
  ...rest
}: SearchableSelectProps) {
  const inputPassthrough = rest as unknown as Omit<
    ComponentProps<"input">,
    "value" | "defaultValue" | "onChange" | "onBlur" | "onFocus" | "onKeyDown" | "type" | "children"
  >;
  const options = useMemo(() => parseOptions(children), [children]);
  const isControlled = valueProp !== undefined;
  const selectableOptions = useMemo(() => options.filter((option) => !option.disabled), [options]);
  const placeholderOption = useMemo(
    () => options.find((option) => option.value === "" && option.disabled),
    [options],
  );
  const defaultResolvedValue = useMemo(() => {
    const normalizedDefault = normalizeValue(defaultValue);
    if (normalizedDefault) {
      return normalizedDefault;
    }

    if (options.some((option) => option.value === "")) {
      return "";
    }

    return selectableOptions[0]?.value ?? "";
  }, [defaultValue, options, selectableOptions]);

  const [internalValue, setInternalValue] = useState(defaultResolvedValue);
  const selectedValue = isControlled ? normalizeValue(valueProp) : internalValue;
  const selectedOption = useMemo(
    () => options.find((option) => option.value === selectedValue) ?? null,
    [options, selectedValue],
  );
  const selectedLabel = selectedOption?.label ?? selectedValue;
  const [query, setQuery] = useState(selectedLabel);
  const [open, setOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(0);
  const wrapperRef = useRef<HTMLDivElement | null>(null);
  const inputRef = useRef<HTMLInputElement | null>(null);
  const listboxId = useId();

  useEffect(() => {
    setQuery(selectedLabel);
  }, [selectedLabel]);

  useEffect(() => {
    if (isControlled) {
      return;
    }

    if (options.some((option) => option.value === internalValue)) {
      return;
    }

    if (options.some((option) => option.value === "")) {
      setInternalValue("");
      return;
    }

    setInternalValue(selectableOptions[0]?.value ?? "");
  }, [internalValue, isControlled, options, selectableOptions]);

  useEffect(() => {
    function handlePointerDown(event: MouseEvent) {
      if (!wrapperRef.current?.contains(event.target as Node)) {
        setOpen(false);
        setQuery(selectedLabel);
        inputRef.current?.setCustomValidity("");
      }
    }

    document.addEventListener("mousedown", handlePointerDown);
    return () => document.removeEventListener("mousedown", handlePointerDown);
  }, [selectedLabel]);

  const filteredOptions = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    if (!normalizedQuery) {
      return selectableOptions;
    }

    return selectableOptions.filter((option) => option.searchText.includes(normalizedQuery));
  }, [query, selectableOptions]);

  const clampedActiveIndex = filteredOptions.length === 0 ? 0 : Math.min(activeIndex, filteredOptions.length - 1);
  const inputValue = open ? (query || selectedLabel) : selectedLabel;
  const placeholder = placeholderOption?.label ?? "Select...";

  function commitValue(nextValue: string) {
    const nextOption = options.find((option) => option.value === nextValue) ?? null;
    if (!isControlled) {
      setInternalValue(nextValue);
    }

    setQuery(nextOption?.label ?? nextValue);
    setOpen(false);
    inputRef.current?.setCustomValidity("");
    onChange?.(createSelectChangeEvent(nextValue, name));
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    onKeyDown?.(event as unknown as KeyboardEvent<HTMLSelectElement>);
    if (event.defaultPrevented || disabled) {
      return;
    }

    if (event.key === "ArrowDown") {
      event.preventDefault();
      setOpen(true);
      setActiveIndex(Math.min(clampedActiveIndex + 1, Math.max(filteredOptions.length - 1, 0)));
      return;
    }

    if (event.key === "ArrowUp") {
      event.preventDefault();
      setOpen(true);
      setActiveIndex(Math.max(clampedActiveIndex - 1, 0));
      return;
    }

    if (event.key === "Escape") {
      event.preventDefault();
      setOpen(false);
      setQuery(selectedLabel);
      return;
    }

    if (event.key === "Enter" && open && filteredOptions[clampedActiveIndex]) {
      event.preventDefault();
      commitValue(filteredOptions[clampedActiveIndex].value);
    }
  }

  return (
    <div ref={wrapperRef} className="relative">
      <input
        {...inputPassthrough}
        ref={inputRef}
        id={id}
        type="text"
        value={inputValue}
        placeholder={placeholder}
        className={cx(inputClassName, className)}
        disabled={disabled}
        autoFocus={autoFocus}
        autoComplete="off"
        role="combobox"
        aria-expanded={open}
        aria-controls={listboxId}
        aria-autocomplete="list"
        aria-haspopup="listbox"
        onFocus={(event) => {
          inputRef.current?.setCustomValidity("");
          setQuery("");
          setOpen(true);
          setActiveIndex(0);
          onFocus?.(event as unknown as FocusEvent<HTMLSelectElement>);
        }}
        onBlur={(event) => {
          if (!wrapperRef.current?.contains(event.relatedTarget as Node | null)) {
            setOpen(false);
            setQuery(selectedLabel);
          }
          onBlur?.(event as unknown as FocusEvent<HTMLSelectElement>);
        }}
        onChange={(event) => {
          inputRef.current?.setCustomValidity("");
          setQuery(event.target.value);
          setOpen(true);
          setActiveIndex(0);
        }}
        onKeyDown={handleKeyDown}
      />

      <select
        value={selectedValue}
        onChange={() => undefined}
        tabIndex={-1}
        aria-hidden="true"
        name={name}
        form={form}
        required={required}
        disabled={disabled}
        className="pointer-events-none absolute left-0 top-0 h-px w-px opacity-0"
        onInvalid={(event) => {
          event.preventDefault();
          const message = event.currentTarget.validationMessage || "Select a value.";
          inputRef.current?.setCustomValidity(message);
          inputRef.current?.reportValidity();
          inputRef.current?.focus();
        }}
      >
        {options.map((option) => (
          <option key={option.key} value={option.value} disabled={option.disabled}>
            {option.label}
          </option>
        ))}
      </select>

      {open ? (
        <div
          id={listboxId}
          role="listbox"
          className="absolute z-30 mt-1 w-full overflow-hidden rounded-md border border-[var(--input-border)] bg-[var(--surface)] shadow-[var(--shadow-card)]"
        >
          <div className="max-h-64 overflow-auto py-1">
            {filteredOptions.length > 0 ? (
              filteredOptions.map((option, index) => {
                const active = index === clampedActiveIndex;
                const selected = option.value === selectedValue;
                return (
                  <button
                    key={option.key}
                    type="button"
                    role="option"
                    tabIndex={-1}
                    aria-selected={selected}
                    className={cx(
                      "block w-full px-2.5 py-1.5 text-left text-[13px] transition-colors",
                      active ? "bg-[var(--surface-soft)]" : "",
                      selected ? "text-[var(--link)]" : "text-[var(--foreground)]",
                    )}
                    onMouseDown={(event) => {
                      event.preventDefault();
                      commitValue(option.value);
                    }}
                  >
                    {option.label}
                  </button>
                );
              })
            ) : (
              <div className="px-2.5 py-1.5 text-[13px] text-[var(--muted-foreground)]">No results found.</div>
            )}
          </div>
        </div>
      ) : null}
    </div>
  );
}
