import Image from "next/image";
import { brandAssets, company } from "@/lib/company";

type Asset = { light: string; dark: string; width: number; height: number };

const assets: Record<"lockup" | "mark" | "wordmark", Asset> = {
  lockup: { light: brandAssets.logo, dark: brandAssets.logoDark, width: 580, height: 361 },
  mark: { light: brandAssets.mark, dark: brandAssets.markDark, width: 258, height: 182 },
  wordmark: { light: brandAssets.wordmark, dark: brandAssets.wordmarkDark, width: 580, height: 173 },
};

type ThemedImageProps = {
  asset: Asset;
  className: string;
  alt: string;
  priority: boolean;
};

/** Renders the light-ink copy under the `.dark` theme class so the charcoal wordmark stays legible. */
function ThemedImage({ asset, className, alt, priority }: ThemedImageProps) {
  return (
    <>
      <Image
        src={asset.light}
        alt={alt}
        width={asset.width}
        height={asset.height}
        priority={priority}
        className={`${className} object-contain dark:hidden`}
      />
      <Image
        src={asset.dark}
        alt=""
        aria-hidden="true"
        width={asset.width}
        height={asset.height}
        priority={priority}
        className={`${className} object-contain hidden dark:block`}
      />
    </>
  );
}

type BrandLogoProps = {
  /**
   * `lockup` stacks the mark above the wordmark (the supplied artwork),
   * `horizontal` sets them side by side for narrow bars, `mark` is the diamond only.
   */
  variant?: "lockup" | "horizontal" | "mark";
  /**
   * Sizing classes. For `lockup`/`mark` they apply to the artwork (e.g. `h-9 w-auto`);
   * for `horizontal` they size the wrapper, so pass a height only (e.g. `h-9`).
   */
  className?: string;
  priority?: boolean;
};

export function BrandLogo({ variant = "lockup", className = "h-8 w-auto", priority = false }: BrandLogoProps) {
  if (variant === "horizontal") {
    return (
      <span className={`inline-flex items-center gap-2 ${className}`} role="img" aria-label={company.name}>
        <ThemedImage asset={assets.mark} className="h-full w-auto" alt="" priority={priority} />
        {/* the wordmark reads heavier than the diamond, so hold it a little shorter */}
        <ThemedImage asset={assets.wordmark} className="h-[70%] w-auto" alt="" priority={priority} />
      </span>
    );
  }

  return <ThemedImage asset={assets[variant]} className={className} alt={company.name} priority={priority} />;
}
