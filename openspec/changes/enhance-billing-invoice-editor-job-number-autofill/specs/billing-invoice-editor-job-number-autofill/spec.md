## ADDED Requirements

### Requirement: Billing invoice editor SHALL parse supported Job Number expressions
The billing invoice editor SHALL accept an empty Job Number value or a Job Number expression composed of canonical `orderNumber-jobSuffix` tokens separated by commas, with optional slash expansion of additional suffixes that share the same order number prefix.

#### Scenario: Empty Job Number leaves manual editing unchanged
- **WHEN** the user leaves the Job Number field empty
- **THEN** the system SHALL not attempt job lookup or generate invoice items from job data

#### Scenario: Slash expansion produces multiple canonical job numbers
- **WHEN** the user enters `168824-1/2/3`
- **THEN** the system SHALL expand the value into the ordered canonical job numbers `168824-1`, `168824-2`, and `168824-3`

#### Scenario: Mixed comma and slash forms preserve order
- **WHEN** the user enters `168824-1/2/3, 168825-1`
- **THEN** the system SHALL resolve the ordered canonical job numbers `168824-1`, `168824-2`, `168824-3`, and `168825-1`

#### Scenario: Unsupported syntax is rejected
- **WHEN** the user enters a Job Number expression that does not match the supported comma-and-slash syntax
- **THEN** the system SHALL show a validation error and SHALL not generate invoice items from that value

### Requirement: Billing invoice editor SHALL resolve each canonical Job Number from the JobListView data source family
For a valid Job Number expression, the system SHALL resolve each canonical Job Number against the same job-order source family used by JobListView and SHALL generate at most one invoice line item per resolved Job Number.

#### Scenario: All job numbers resolve successfully
- **WHEN** the user enters `168824-1, 168825-1` and both job numbers are found in the lookup source
- **THEN** the system SHALL generate exactly two invoice line items in the same order as the parsed job numbers

#### Scenario: A job number cannot be resolved
- **WHEN** at least one canonical Job Number is not found in the lookup source
- **THEN** the system SHALL report which job numbers could not be resolved and SHALL not silently generate a line item for those missing jobs

#### Scenario: Duplicate canonical job numbers are provided
- **WHEN** parsing produces the same canonical Job Number more than once
- **THEN** the system SHALL de-duplicate the lookup set and SHALL generate only one invoice line item for that canonical Job Number

### Requirement: Generated invoice items SHALL use Purchase Order and section 1 Product Details content
For each resolved Job Number, the generated invoice line item SHALL set `P.O. Number` from the job's Purchase Order and SHALL set `Description` from section 1 of Product Details after removing the section header line.

#### Scenario: Product Details section 1 is converted into description text
- **WHEN** a resolved job has Product Details whose first section begins with `1.印刷內容：`
- **THEN** the generated invoice item description SHALL include the remaining lines of section 1 up to, but not including, the next numbered section

#### Scenario: Generated line item uses purchase order directly
- **WHEN** a resolved job has Purchase Order `FL-BU-26-0161~164`
- **THEN** the generated invoice item's `P.O. Number` SHALL be `FL-BU-26-0161~164`

#### Scenario: Section 1 content is unavailable
- **WHEN** a resolved job does not have extractable section 1 Product Details content
- **THEN** the system SHALL surface that job as requiring manual review instead of generating a misleading description

### Requirement: Generated line items SHALL remain editable after autofill
After autofill completes, the invoice editor SHALL keep the generated line items in the editable grid so the user can review, adjust, add, or remove rows before saving.

#### Scenario: User edits generated data before save
- **WHEN** autofill has generated one or more invoice line items
- **THEN** the user SHALL be able to edit the generated `P.O. Number` and `Description` fields before saving the invoice

#### Scenario: Regenerating refreshes the generated set
- **WHEN** the user changes the Job Number expression and runs autofill again
- **THEN** the system SHALL replace the previously generated auto-filled set with a new set that matches the latest parsed job numbers
