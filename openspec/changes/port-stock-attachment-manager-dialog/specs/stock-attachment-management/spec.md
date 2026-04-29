## ADDED Requirements

### Requirement: System SHALL Open A Reusable Stock Attachment Dialog From Stock Actions
The system SHALL provide one reusable stock attachment management dialog that can be launched from both StockView and ProductRecordDialog for the active stock product.

#### Scenario: Launch from StockView attachment action
- **WHEN** a user triggers the Attachment action for a selected stock product in StockView
- **THEN** the system SHALL open the stock attachment dialog bound to that product identity

#### Scenario: Launch from ProductRecordDialog attachment action
- **WHEN** a user triggers the Attachment action in ProductRecordDialog while editing a product
- **THEN** the system SHALL open the same stock attachment dialog bound to the current product identity

### Requirement: Stock Attachment Dialog SHALL Present Legacy-Parity Thumbnail Size Modes
The stock attachment dialog SHALL support four thumbnail display modes matching legacy semantics: small, medium, large, and x-large.

#### Scenario: Switch to small mode
- **WHEN** the user selects small mode
- **THEN** the system SHALL render attachment thumbnails using the small preset dimensions

#### Scenario: Switch to x-large mode
- **WHEN** the user selects x-large mode
- **THEN** the system SHALL render attachment thumbnails using the x-large preset dimensions

### Requirement: System SHALL Support Upload And Refresh Of Product Attachments
The system SHALL allow users to upload one or more attachment files for the active product and SHALL refresh attachment listing state after upload completes.

#### Scenario: Successful upload adds attachment entries
- **WHEN** a user uploads valid attachment file(s)
- **THEN** the system SHALL persist the attachment metadata/content for the active product and SHALL display the uploaded files in the dialog list

#### Scenario: Upload exceeds configured file size
- **WHEN** a selected file exceeds configured upload constraints
- **THEN** the system SHALL reject that upload and SHALL present an actionable validation error

### Requirement: System SHALL Support Multi-Selection Download And Delete With Confirmation
The system SHALL support selecting multiple attachments and executing batch download or batch delete actions, with confirmation required before delete.

#### Scenario: Download selected attachments
- **WHEN** one or more attachments are selected and the user invokes download
- **THEN** the system SHALL start download for each selected attachment using the original file names

#### Scenario: Confirmed delete removes selected attachments
- **WHEN** one or more attachments are selected, the user confirms delete, and the operation succeeds
- **THEN** the system SHALL remove the selected attachment records from storage and refresh the dialog list

#### Scenario: Delete canceled by user
- **WHEN** one or more attachments are selected and the user cancels the delete confirmation
- **THEN** the system SHALL perform no deletion and preserve the current selection/list state

### Requirement: System SHALL Provide File-Type-Aware Attachment Preview Behavior
The system SHALL provide preview behavior based on attachment type, including inline image preview and supported fallback for non-image files.

#### Scenario: Preview image attachment
- **WHEN** the user opens an attachment whose file type is a supported image format
- **THEN** the system SHALL display the image content in an inline viewer pane/dialog

#### Scenario: Preview PDF attachment
- **WHEN** the user opens a PDF attachment
- **THEN** the system SHALL attempt inline PDF viewing and SHALL provide download/open fallback if inline rendering is unavailable

#### Scenario: Preview unsupported non-image file type
- **WHEN** the user opens an attachment whose type is not directly previewable
- **THEN** the system SHALL provide a deterministic open/download action instead of failing silently
