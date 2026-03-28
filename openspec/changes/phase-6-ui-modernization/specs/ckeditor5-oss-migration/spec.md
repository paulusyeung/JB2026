## ADDED Requirements

### Requirement: Rich-Text Editor Must Use CKEditor 5 Open-Source Build
The proprietary CKEditor licence MUST be replaced with the `@ckeditor/ckeditor5-build-classic` open-source (GPL v2) package. No CKEditor premium plugins MAY be included.

#### Scenario: Dependency scan finds no proprietary CKEditor packages
- **WHEN** the CI pipeline scans `package.json` and the production bundle
- **THEN** zero proprietary CKEditor 4 or premium plugin packages SHALL be present

### Requirement: CKEditor 5 Must Preserve Existing HTML Content
Content created in CKEditor 4 MUST render without data loss in CKEditor 5.

#### Scenario: Legacy HTML content opens correctly in CKEditor 5
- **WHEN** existing rich-text content saved by CKEditor 4 is loaded into the CKEditor 5 editor component
- **THEN** the rendered output SHALL match the original content without stripping tags supported by CKEditor 5's default schema

### Requirement: Rich-Text Toolbar Must Include Bold, Italic, Lists, Tables, and Links
The CKEditor 5 instance MUST be configured with at minimum: bold, italic, unordered list, ordered list, table insertion, and hyperlink plugins.

#### Scenario: Toolbar buttons are present and functional
- **WHEN** the editor is rendered in the browser
- **THEN** toolbar buttons for bold, italic, bullet list, numbered list, insert table, and insert link SHALL all be visible and operable
