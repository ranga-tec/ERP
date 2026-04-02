"use client";

import { useEffect, useId, useMemo, useRef, useState, type KeyboardEvent, type ReactNode } from "react";
import type { DataGridOption } from "./types";

type LookupCellProps = {
  value: string;
  options: DataGridOption[];
  placeholder?: string;
  searchPlaceholder?: string;
  noOptionsLabel?: string;
  className?: string;
  disabled?: boolean;
  submitOnEnter?: boolean;
  onChange: (value: string) => void;
  onSubmit?: () => void;
  renderOption?: (
    option: DataGridOption,
    state: { active: boolean; selected: boolean },
  ) => ReactNode;
};

const inputClassName =
  "w-full rounded-xl border border-[var(--input-border)] bg-[var(--surface)] px-3 py-2 text-sm text-[var(--foreground)] shadow-[var(--shadow-control)] outline-none transition focus-visible:border-[var(--link)] focus-visible:ring-2 focus-visible:ring-[var(--ring-accent)] placeholder:text-[var(--muted-foreground)]/80";

function cx(...parts: Array<string | null | undefined | false>) {
  return parts.filter(Boolean).join(" ");
}

function optionSearchText(option: DataGridOption): string {
  return [
    option.label,
    option.description ?? "",
    option.searchText ?? "",
    ...(option.keywords ?? []),
  ]
    .join(" ")
    .toLowerCase();
}

export function LookupCell({
  value,
  options,
  placeholder,
  searchPlaceholder,
  noOptionsLabel = "No results found.",
  className,
  disabled,
  submitOnEnter,
  onChange,
  onSubmit,
  renderOption,
}: LookupCellProps) {
  const wrapperRef = useRef<HTMLDivElement | null>(null);
  const listboxId = useId();
  const selectedOption = useMemo(() => options.find((option) => option.value === value) ?? null, [options, value]);
  const [query, setQuery] = useState(selectedOption?.label ?? "");
  const [open, setOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(0);

  useEffect(() => {
    function handlePointerDown(event: MouseEvent) {
      if (!wrapperRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    }

    document.addEventListener("mousedown", handlePointerDown);
    return () => document.removeEventListener("mousedown", handlePointerDown);
  }, []);

  const filteredOptions = useMemo(() => {
    const normalizedQuery = query.trim().toLowerCase();
    if (!normalizedQuery) {
      return options;
    }

    return options.filter((option) => optionSearchText(option).includes(normalizedQuery));
  }, [options, query]);

  const clampedActiveIndex = filteredOptions.length === 0 ? 0 : Math.min(activeIndex, filteredOptions.length - 1);
  const inputValue = open ? query : selectedOption?.label ?? "";

  function selectOption(option: DataGridOption) {
    onChange(option.value);
    setQuery(option.label);
    setOpen(false);
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (disabled) {
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
      setQuery(selectedOption?.label ?? "");
      return;
    }

    if (event.key !== "Enter") {
      return;
    }

    if (open && filteredOptions[clampedActiveIndex]) {
      event.preventDefault();
      selectOption(filteredOptions[clampedActiveIndex]);
      return;
    }

    if (submitOnEnter === false || !onSubmit) {
      return;
    }

    event.preventDefault();
    onSubmit();
  }

  return (
    <div ref={wrapperRef} className="relative">
      <input
        value={inputValue}
        onFocus={() => {
          setQuery(selectedOption?.label ?? "");
          setOpen(true);
        }}
        onChange={(event) => {
          setQuery(event.target.value);
          setOpen(true);
          setActiveIndex(0);
        }}
        onKeyDown={handleKeyDown}
        placeholder={searchPlaceholder ?? placeholder}
        className={cx(inputClassName, className)}
        disabled={disabled}
        autoComplete="off"
        aria-expanded={open}
        aria-controls={listboxId}
        aria-autocomplete="list"
        role="combobox"
      />

      {open ? (
        <div
          id={listboxId}
          role="listbox"
          className="absolute z-30 mt-1 w-full overflow-hidden rounded-xl border border-[var(--input-border)] bg-[var(--surface)] shadow-xl"
        >
          <div className="max-h-64 overflow-auto py-1">
            {filteredOptions.length > 0 ? (
              filteredOptions.map((option, index) => {
                const active = index === clampedActiveIndex;
                const selected = option.value === value;
                return (
                  <button
                    key={option.value}
                    type="button"
                    role="option"
                    aria-selected={selected}
                    className={cx(
                      "block w-full px-3 py-2 text-left text-sm transition",
                      active ? "bg-[var(--surface-soft)]" : "",
                      selected ? "text-[var(--link)]" : "text-[var(--foreground)]",
                    )}
                    onMouseDown={(event) => {
                      event.preventDefault();
                      selectOption(option);
                    }}
                  >
                    {renderOption ? (
                      renderOption(option, { active, selected })
                    ) : (
                      <>
                        <div className="font-medium">{option.label}</div>
                        {option.description ? (
                          <div className="mt-0.5 text-xs text-[var(--muted-foreground)]">{option.description}</div>
                        ) : null}
                      </>
                    )}
                  </button>
                );
              })
            ) : (
              <div className="px-3 py-2 text-sm text-[var(--muted-foreground)]">{noOptionsLabel}</div>
            )}
          </div>
        </div>
      ) : null}
    </div>
  );
}
