## ADDED Requirements

### Requirement: User can configure job-order print options before PDF generation
The system SHALL present a print-manager workflow before generating a job-order PDF from supported job-order screens. The workflow SHALL allow the user to review the target order number, choose a supported layout, toggle picture inclusion, toggle product-detail inclusion, and choose which workflows are included in the report.

#### Scenario: Print launched from a supported job-order screen
- **WHEN** the user activates the Print action from JobListView or from a JobOrderForm instance bound to an existing job order
- **THEN** the system opens a job-order print manager dialog instead of invoking the browser print dialog or immediately requesting a PDF

#### Scenario: Default print options are loaded for the selected job order
- **WHEN** the print manager dialog opens for an existing job order
- **THEN** the system pre-populates the order number, default layout, and the ordered workflow list for that job order

### Requirement: Job-order PDF generation must honor selected print options
The system SHALL generate a PDF for the selected job order using the exact options submitted from the print manager. The generated output SHALL reflect the chosen layout, omit pictures when requested, omit product details when requested, and include only the workflows selected by the user.

#### Scenario: Generate default job-order PDF
- **WHEN** the user submits the print manager with the default layout and no suppression options enabled
- **THEN** the system returns an `application/pdf` response containing the job-order report for the selected order

#### Scenario: Suppress optional sections in the PDF
- **WHEN** the user enables no-picture or no-product-details before submitting the print manager
- **THEN** the generated PDF omits the corresponding picture block or product-detail content from the report output

#### Scenario: Limit report output to selected workflows
- **WHEN** the user submits the print manager with a subset of workflows selected
- **THEN** the generated PDF includes only those workflow sections and excludes unselected workflows

### Requirement: Job-order print output must preserve legacy-compatible report structure
The system SHALL produce a job-order PDF whose observable structure remains compatible with the legacy Job Order report. The output SHALL preserve the core header fields, workflow section ordering, remarks area, and attachment-image behavior required for operational use.

#### Scenario: Default report includes legacy header fields
- **WHEN** the system generates the default job-order PDF
- **THEN** the report includes the composite order number, customer name, order title, customer reference, product code or equivalent mapped identifier, ordered dates, required date, invoice reference, and invoice amount

#### Scenario: Workflow sections remain ordered deterministically
- **WHEN** the report includes multiple workflows
- **THEN** the workflow sections appear in the same deterministic order used by the print manager selection list

### Requirement: Print failures must be actionable to the user and diagnosable in the service
The system SHALL surface job-order print failures with a user-visible error state and SHALL record sufficient backend diagnostics to troubleshoot option-specific print errors.

#### Scenario: Frontend handles print request failure
- **WHEN** the job-order print request fails
- **THEN** the user receives a localized error message and the print manager remains recoverable without reloading the page

#### Scenario: Backend records print failure context
- **WHEN** PDF generation fails for a job-order print request
- **THEN** the service records the order identifier and submitted print-option context in logs without exposing sensitive data in the client response