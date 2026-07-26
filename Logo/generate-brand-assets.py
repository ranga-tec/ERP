"""Derive the app/PDF brand assets from the supplied artwork in this folder.

Run from the repository root after replacing `Logo/image.png`:

    python Logo/generate-brand-assets.py

Outputs:
    backend/src/ISS.Infrastructure/Assets/company-logo.png   -> PDF letterhead (embedded resource)
    frontend/public/brand/*.png                              -> UI logo, wordmark and mark (light + dark ink)
    frontend/src/app/icon.png, favicon.ico                   -> browser tab icon

Requires Pillow (`pip install pillow`).
"""

import os

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "Logo", "image.png")
BACKEND_ASSETS = os.path.join(ROOT, "backend", "src", "ISS.Infrastructure", "Assets")
PUBLIC_BRAND = os.path.join(ROOT, "frontend", "public", "brand")
APP_DIR = os.path.join(ROOT, "frontend", "src", "app")

# Crop boxes for the supplied 613x428 artwork; adjust if the source changes.
CROP_LOCKUP = (10, 24, 590, 385)
CROP_MARK = (163, 22, 421, 204)
CROP_WORDMARK = (10, 212, 590, 385)

LIGHT_INK = (233, 240, 249)  # stands in for the charcoal ink on dark surfaces


def dewhite(im: Image.Image) -> Image.Image:
    """Turn the pure-white studio background into alpha, keeping brand colours exact."""
    im = im.convert("RGB")
    out = Image.new("RGBA", im.size)
    src, dst = im.load(), out.load()
    width, height = im.size
    for y in range(height):
        for x in range(width):
            r, g, b = src[x, y]
            a = (255 - min(r, g, b)) / 255.0
            if a <= 0.03:
                dst[x, y] = (0, 0, 0, 0)
            elif a >= 0.80:
                dst[x, y] = (r, g, b, 255)
            else:
                # un-premultiply against the white backdrop so antialiased edges stay clean
                inv = 1.0 - a
                dst[x, y] = (
                    max(0, min(255, int((r - 255 * inv) / a))),
                    max(0, min(255, int((g - 255 * inv) / a))),
                    max(0, min(255, int((b - 255 * inv) / a))),
                    int(a * 255),
                )
    return out


def to_dark_variant(im: Image.Image) -> Image.Image:
    """Recolour the charcoal ink to a light ink; leave the logo orange untouched."""
    out = im.copy()
    px = out.load()
    width, height = out.size
    for y in range(height):
        for x in range(width):
            r, g, b, a = px[x, y]
            if a and r - b < 60:  # not orange -> neutral ink
                px[x, y] = (*LIGHT_INK, a)
    return out


def save(im: Image.Image, *paths: str) -> None:
    for path in paths:
        os.makedirs(os.path.dirname(path), exist_ok=True)
        im.save(path, optimize=True)
        print(f"wrote {os.path.relpath(path, ROOT)} {im.size}")


def main() -> None:
    source = dewhite(Image.open(SRC))
    lockup = source.crop(CROP_LOCKUP)
    mark = source.crop(CROP_MARK)
    wordmark = source.crop(CROP_WORDMARK)

    save(lockup, os.path.join(BACKEND_ASSETS, "company-logo.png"), os.path.join(PUBLIC_BRAND, "c-com-logo.png"))
    save(to_dark_variant(lockup), os.path.join(PUBLIC_BRAND, "c-com-logo-dark.png"))
    save(mark, os.path.join(PUBLIC_BRAND, "c-com-mark.png"))
    save(to_dark_variant(mark), os.path.join(PUBLIC_BRAND, "c-com-mark-dark.png"))
    save(wordmark, os.path.join(PUBLIC_BRAND, "c-com-wordmark.png"))
    save(to_dark_variant(wordmark), os.path.join(PUBLIC_BRAND, "c-com-wordmark-dark.png"))

    side = max(mark.size) + 16
    icon = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    icon.paste(mark, ((side - mark.size[0]) // 2, (side - mark.size[1]) // 2), mark)
    icon = icon.resize((256, 256), Image.LANCZOS)
    save(icon, os.path.join(APP_DIR, "icon.png"))
    icon.save(os.path.join(APP_DIR, "favicon.ico"), sizes=[(16, 16), (32, 32), (48, 48), (64, 64)])
    print("wrote frontend/src/app/favicon.ico")


if __name__ == "__main__":
    main()
