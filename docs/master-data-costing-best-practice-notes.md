# Master Data and Costing Design Notes (Industry Practice)

## Implemented in this repository

The following patterns from the references are now implemented:

- UoM master and UoM conversion master (`/api/uoms`, `/api/uom-conversions`)
- Payment type master (`/api/payment-types`)
- Tax code master and tax conversion rules (`/api/taxes`, `/api/tax-conversions`)
- Currency master and effective-dated rates (`/api/currencies`, `/api/currency-rates`)
- Reference form master (`/api/reference-forms`)
- Costing report with weighted average, last receipt cost, and on-hand valuation (`/api/reporting/costing`, `/reporting/costing`)

These are implemented as configurable masters so transaction forms can consume centrally-managed values.

This implementation follows practical ERP patterns from major platforms and standards:

- Unit conversions: maintain explicit conversion factors between units, and convert operational quantities to base units for posting.
  - Source: Microsoft Dynamics 365 Supply Chain Management (Manage unit of measure conversions)
  - https://learn.microsoft.com/en-us/dynamics365/supply-chain/pim/tasks/manage-unit-measure-conversions

- Payment methods/types: maintain a payment method master and classify operational payments by method.
  - Source: Microsoft Dynamics 365 Business Central (Set up payment methods)
  - https://learn.microsoft.com/en-us/dynamics365/business-central/finance-payment-methods

- Tax setup: separate tax codes/rates from transactions and support configurable mapping/rules between tax treatments.
  - Source: Oracle E-Business Tax (Tax statuses, rates, and recovery rules)
  - https://docs.oracle.com/cd/E18727_01/doc.121/e13629/T459462T564683.htm

- Currency and FX conversion: use ISO 4217 currency definitions and effective-dated conversion rates.
  - Sources:
    - ISO 4217 overview
      - https://www.iso.org/iso-4217-currency-codes.html
    - Oracle Cloud Financials (cross-currency conversion)
      - https://docs.oracle.com/en/cloud/saas/financials/24b/faufa/how-cross-currency-journals-are-converted.html

- Costing/valuation: inventory cost reporting should support cost formulas like weighted average and be transparent at item level.
  - Source: IAS 2 Inventories
  - https://www.ifrs.org/issued-standards/list-of-standards/ias-2-inventories/

- Exchange-rate accounting context for reporting/base currency design:
  - Source: IAS 21 The Effects of Changes in Foreign Exchange Rates
  - https://www.ifrs.org/issued-standards/list-of-standards/ias-21-the-effects-of-changes-in-foreign-exchange-rates/
