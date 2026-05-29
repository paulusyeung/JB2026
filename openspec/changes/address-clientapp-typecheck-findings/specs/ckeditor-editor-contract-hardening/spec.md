## ADDED Requirements

### Requirement: CKEditor integrations SHALL match the installed editor contract
All ClientApp CKEditor integrations MUST pass editor constructors and bindings that conform to the installed `@ckeditor/ckeditor5-vue` contract used in the workspace.

#### Scenario: CKEditor component is rendered
- **WHEN** a form component passes an editor instance or constructor into the CKEditor Vue component
- **THEN** the value satisfies the wrapper's expected editor contract
- **AND** the component compiles without local `unknown`-based casts that hide type incompatibilities

### Requirement: CKEditor adapter types SHALL be centralized
If the third-party package boundary requires local adaptation, the adaptation MUST be defined in a shared integration boundary rather than repeated at each call site.

#### Scenario: multiple forms use CKEditor
- **WHEN** more than one ClientApp component renders CKEditor-backed content
- **THEN** the editor compatibility typing is defined in one shared place
- **AND** downstream forms reuse that contract consistently
