## ADDED Requirements

### Requirement: Shared UI components SHALL expose caller-safe generic contracts
Shared ClientApp UI components such as mobile list/card, action menu, and workflow/status controls MUST expose prop, slot, event, and generic contracts that match their supported caller data shapes.

#### Scenario: typed view model is passed into shared mobile card component
- **WHEN** a view passes a typed item array and typed column definitions into a shared list/card component
- **THEN** the shared component accepts the typed data without requiring unsafe casts to `Record<string, unknown>`
- **AND** the view compiles with the component's declared generic contract

### Requirement: Shared event handlers SHALL use framework-compatible signatures
Shared UI interactions MUST accept event signatures that match the Vuetify or Vue component emitting them, or use explicit wrapper callbacks to transform the event into domain data.

#### Scenario: menu or list item click opens a domain action
- **WHEN** a Vuetify list or menu click triggers a domain operation
- **THEN** the bound handler accepts the emitted event signature directly or through a wrapper function
- **AND** the component does not rely on mismatched parameter types
