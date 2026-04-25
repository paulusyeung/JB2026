## ADDED Requirements

### Requirement: Delete action shall be available in stock list and product record flows
The system SHALL provide an executable delete action in both `StockView` and `ProductRecordDialog` for users authorized to delete products.

#### Scenario: Delete from stock list single selection
- **WHEN** a user selects one product row in `StockView` and confirms delete
- **THEN** the system SHALL execute the product delete lifecycle for that selected product

#### Scenario: Delete from product record dialog
- **WHEN** a user opens `ProductRecordDialog` in edit mode and confirms delete
- **THEN** the system SHALL execute the product delete lifecycle for the dialog product

### Requirement: Delete lifecycle shall follow retire-then-hard-delete semantics
The system SHALL preserve legacy delete lifecycle behavior such that first-pass delete retires an active product and subsequent delete on a retired product permanently removes it.

#### Scenario: First delete request retires active product
- **WHEN** a delete request targets a product with `Retired = false`
- **THEN** the system SHALL set retire metadata and persist the product as retired instead of permanently deleting it

#### Scenario: Delete request on retired product performs hard delete
- **WHEN** a delete request targets a product with `Retired = true`
- **THEN** the system SHALL permanently delete the product and return a `hardDeleted` lifecycle outcome

### Requirement: Hard delete shall cascade dependent cleanup
The system SHALL remove dependent stock movement records, attachment metadata, and associated files when performing hard delete on a retired product.

#### Scenario: Hard delete removes stock in/out rows
- **WHEN** hard delete is executed for a retired product
- **THEN** all stock in/out rows associated with that product SHALL be removed

#### Scenario: Hard delete removes attachments and files
- **WHEN** hard delete is executed for a retired product with attachments
- **THEN** product attachment records and corresponding files SHALL be removed before final product deletion completes

### Requirement: Stock list checkbox mode shall support multi-item delete processing
The system SHALL support checkbox-based multi-item deletion in `StockView` and process each selected product using the same lifecycle rules.

#### Scenario: Batch delete with mixed lifecycle outcomes
- **WHEN** a user confirms delete for multiple selected products
- **THEN** each selected product SHALL be processed independently and the UI SHALL present an aggregate outcome summary

### Requirement: Delete operations shall require confirmation and return lifecycle-aware feedback
The system SHALL request user confirmation before executing delete and SHALL surface outcome-aware feedback indicating whether the result is retire or hard delete.

#### Scenario: User cancels delete confirmation
- **WHEN** a user declines the delete confirmation prompt
- **THEN** no delete action SHALL be executed and data SHALL remain unchanged

#### Scenario: User confirms delete and receives lifecycle result
- **WHEN** delete completes successfully
- **THEN** the UI SHALL display a success message that reflects the returned lifecycle outcome (`retired` or `hardDeleted`)

### Requirement: UI state shall refresh after delete completion
The system SHALL refresh relevant stock list and record views after delete completion to ensure displayed data matches server state.

#### Scenario: Stock list refresh after list delete
- **WHEN** delete is executed from `StockView`
- **THEN** the list selection and displayed rows SHALL refresh to reflect removed/retired products

#### Scenario: Dialog closes after successful record delete
- **WHEN** delete is executed from `ProductRecordDialog`
- **THEN** the dialog SHALL close and the parent stock list SHALL refresh
