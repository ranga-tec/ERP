# Branding (C-COM Equipment)

The system is branded for **C-COM Equipment (Pvt) Ltd**. Company identity appears in three places:
generated PDF documents, the web UI, and outbound email.

## Company details

| Field | Value |
| --- | --- |
| Name | C-COM Equipment (Pvt) Ltd |
| Address | 64/10 Nawala Road, Nugegoda, Sri Lanka |
| Phone | (+94) 112812994 |
| Email | info@c-com.lk |
| Website | www.c-com.lk |
| Logo orange | `#F7941C` |
| Logo charcoal | `#231F20` |

## Source artwork and generated assets

`Logo/image.png` is the supplied artwork (white background). Every derived asset is produced by
`Logo/generate-brand-assets.py`, which strips the white backdrop to alpha and emits a light-ink
variant for dark surfaces:

```bash
python Logo/generate-brand-assets.py
```

| Output | Used by |
| --- | --- |
| `backend/src/ISS.Infrastructure/Assets/company-logo.png` | PDF letterhead (embedded resource) |
| `frontend/public/brand/c-com-logo{,-dark}.png` | stacked lockup — login screen |
| `frontend/public/brand/c-com-wordmark{,-dark}.png` | horizontal lockup — expanded sidebar |
| `frontend/public/brand/c-com-mark{,-dark}.png` | collapsed sidebar rail, mobile header |
| `frontend/src/app/icon.png`, `favicon.ico` | browser tab icon |

Replacing the artwork means dropping a new `Logo/image.png`, adjusting the crop boxes at the top of
the script if the proportions changed, and re-running it. The assets are committed, so the script is
only needed when the artwork changes.

## PDF documents

`backend/src/ISS.Infrastructure/Documents/DocumentBranding.cs` holds the letterhead text, colours,
and the embedded-logo loader. It is the only place these strings live on the backend.

`DocumentPdfService.BuildPdf` renders every one of the 25 document types (RFQ, PO, GRN, invoices,
job sheets, credit/debit notes, and so on), so the letterhead applies to all of them:

- **Header** — logo top-left, company name and contact block right-aligned, closed by an orange
  rule; below it the document title, reference number, meta table, QR code and barcode.
- **Footer** — company name, address and contact strip, then the generation timestamp and
  `Page X of Y`.

Both repeat on every page. `ItemLabelPdf` builds its own page layout and calls the same
`Letterhead` / `Footer` helpers.

The logo is an `EmbeddedResource` (see `ISS.Infrastructure.csproj`, pinned to the logical name
`ISS.Infrastructure.Assets.CompanyLogo.png`), so no file needs to ship alongside the container image.

## Web UI

`frontend/src/lib/company.ts` mirrors the company details for the frontend, and
`frontend/src/components/BrandLogo.tsx` renders the artwork. `BrandLogo` swaps to the light-ink
asset under the `.dark` theme class, so the charcoal wordmark stays legible in both themes.

| Surface | Variant |
| --- | --- |
| Login screen (`(auth)/login`) | `lockup` + full contact block under the form |
| Sidebar, expanded | `horizontal` + tagline |
| Sidebar, collapsed | `mark`, stacked above the pin button |
| App header, mobile only | `mark` |
| Tab title / metadata (`app/layout.tsx`) | `C-COM ERP` |

## Email

`SmtpEmailOptions.FromName` defaults to the company name; `appsettings.json` sets
`FromEmail` to `no-reply@c-com.lk`. Override both per environment as usual.

## Not rebranded (deliberate)

- **JWT `Issuer` / `Audience`** (`appsettings.json`) — changing these invalidates every issued token
  and is a deployment concern, not a branding one.
- **Document QR payload prefixes** (`NEUEDGE:PO:{id}` etc.) — these are machine identifiers that
  external scanners may already be matching on.
- **Cookie and localStorage keys** (`neuedge_token`, `neuedge_sidebar_*`) — renaming them signs every
  user out and drops their saved UI preferences.
- **The Excel import template** (`/api/admin/import/template`) — its header rows are parsed on
  upload; decoration would break the importer.
