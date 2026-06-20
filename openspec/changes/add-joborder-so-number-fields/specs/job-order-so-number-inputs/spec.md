## Requirement: API request DTOs MUST include SONumber and OriginalSONumber

The `CreateJobOrderRequest` and `UpdateJobOrderRequest` C# records SHALL include `string? SONumber` and `string? OriginalSONumber` properties with `[StringLength(32)]`.

#### Scenario: CreateJobOrderRequest includes SO number fields
- **GIVEN** the `CreateJobOrderRequest` class
- **THEN** it SHALL define `SONumber` as `string?` with `[StringLength(32)]`
- **THEN** it SHALL define `OriginalSONumber` as `string?` with `[StringLength(32)]`

#### Scenario: UpdateJobOrderRequest includes SO number fields
- **GIVEN** the `UpdateJobOrderRequest` class
- **THEN** it SHALL define `SONumber` as `string?` with `[StringLength(32)]`
- **THEN** it SHALL define `OriginalSONumber` as `string?` with `[StringLength(32)]`

## Requirement: API response DTOs MUST include SONumber and OriginalSONumber

The `JobOrderResponse` and `JobDetailResponse` C# records SHALL include `string? SONumber` and `string? OriginalSONumber` properties.

#### Scenario: JobOrderResponse includes SO number fields
- **GIVEN** the `JobOrderResponse` class
- **THEN** it SHALL define `SONumber` as `string?`
- **THEN** it SHALL define `OriginalSONumber` as `string?`

#### Scenario: Repository maps SO number fields on create
- **GIVEN** a `CreateJobOrderRequest` with `SONumber` set
- **WHEN** `EfJobManagementRepository.CreateJobOrder` is called
- **THEN** the returned `JobOrderResponse` SHALL have `SONumber` matching the request value
- **AND** `OriginalSONumber` SHALL match the request value

#### Scenario: Repository maps SO number fields on update
- **GIVEN** an existing job order
- **WHEN** `EfJobManagementRepository.UpdateJobOrder` is called with updated `SONumber`
- **THEN** the returned `JobOrderResponse` SHALL reflect the updated values

## Requirement: Job Order form MUST display Sales Order Number and Original Sales Order Number

The Job Order form SHALL show two readonly input fields for **Sales Order Number** (`soNumber`) and **Original Sales Order Number** (`originalSONumber`) that reflect the current values from the backend.

#### Scenario: Viewing an existing job with SO numbers
- **WHEN** a user opens a job order that has `sonumber` and `originalSonumber` set in the backend
- **THEN** the form SHALL display those values in the respective readonly input fields
- **THEN** both fields SHALL be visually marked as readonly (disabled editing)

#### Scenario: Viewing a new/empty job
- **WHEN** a user opens a new job order (no orderId)
- **THEN** the Sales Order Number and Original Sales Order Number fields SHALL be empty or blank
- **THEN** both fields SHALL remain readonly

## Requirement: Type definitions MUST include soNumber and originalSONumber

The `JobOrderFormData`, `JobOrderRecord`, and `JobDetail` TypeScript interfaces SHALL include optional `soNumber?: string` and `originalSONumber?: string` properties.

#### Scenario: JobOrderFormData includes SO number fields
- **WHEN** `JobOrderFormData` is imported from `@/types/api`
- **THEN** it SHALL define `soNumber` as an optional string
- **THEN** it SHALL define `originalSONumber` as an optional string

#### Scenario: JobOrderRecord includes SO number fields
- **WHEN** `JobOrderRecord` is imported from `@/types/api`
- **THEN** it SHALL define `soNumber` as an optional string
- **THEN** it SHALL define `originalSONumber` as an optional string

## Requirement: Service layer MUST round-trip SO number fields through save

When saving a job order, the frontend service SHALL include `soNumber` and `originalSONumber` in the request payload so they persist correctly.

#### Scenario: Save includes SO numbers
- **WHEN** `saveJob()` is called with a `JobOrderFormData` that has `soNumber` and/or `originalSONumber` set
- **THEN** the underlying API request SHALL include those fields in the create/update payload
- **THEN** the backend SHALL persist the values (verified by parity tests)

## Requirement: i18n labels MUST exist for all three locales

The job form messages object SHALL contain localized labels for `salesOrderNumber` and `originalSalesOrderNumber` in English, Simplified Chinese, and Traditional Chinese.

#### Scenario: Labels resolve correctly per locale
- **WHEN** the active locale is English (`en`)
- **THEN** `$t('jobForm.fields.salesOrderNumber')` SHALL return `'Sales Order Number'`
- **AND** `$t('jobForm.fields.originalSalesOrderNumber')` SHALL return `'Original Sales Order Number'`

#### Scenario: Labels exist in zhHans
- **WHEN** the active locale is Simplified Chinese (`zhHans`)
- **THEN** both label keys SHALL resolve to non-empty Chinese strings

#### Scenario: Labels exist in zhHant
- **WHEN** the active locale is Traditional Chinese (`zhHant`)
- **THEN** both label keys SHALL resolve to non-empty Chinese strings
