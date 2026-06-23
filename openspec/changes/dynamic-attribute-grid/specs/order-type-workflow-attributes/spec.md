## ADDED Requirements

### Requirement: Backend returns workflow attributes per order type

The system SHALL expose a read-only API endpoint `GET /api/v2/order-types/{orderType}/workflow-attributes` that returns the list of workflow attributes associated with the given order type, ordered by `WorkIndex`.

The response body SHALL be:
```json
{
  "workflowAttributes": [
    {
      "workIndex": 0,
      "workflowName": "印刷用紙",
      "options": ["----", "成昌", "平和", "友邦", "聯興", "建華"]
    }
  ]
}
```

The `options` array SHALL be derived by splitting the `WorkTitle` column from `Z_Workflow` on the `;` delimiter.

#### Scenario: Valid order type returns workflow attributes
- **WHEN** the client sends `GET /api/v2/order-types/0/workflow-attributes`
- **THEN** the server returns HTTP 200 with a JSON body containing a `workflowAttributes` array with items ordered by `workIndex`

#### Scenario: Invalid order type returns empty array
- **WHEN** the client sends `GET /api/v2/order-types/99/workflow-attributes`
- **THEN** the server returns HTTP 200 with an empty `workflowAttributes` array

---

### Requirement: Frontend fetches workflow attributes when order type changes

When the `orderType` field on the form draft changes, `JobOrderForm.vue` SHALL call the backend endpoint to fetch the corresponding workflow attributes. While fetching, the attribute grid SHALL show a loading indicator (e.g., a single row with a progress-linear or skeleton). On failure, the grid SHALL remain empty and a console warning SHALL be logged (no user-facing error toast, to avoid disrupting the form).

#### Scenario: Order type selection triggers attribute fetch
- **WHEN** the user selects a new order type from the dropdown
- **THEN** the component fetches workflow attributes from the API and replaces the attribute grid rows

#### Scenario: Fetch fails gracefully
- **WHEN** the API call fails (network error, server error)
- **THEN** the attribute grid is cleared and a console warning is logged

---

### Requirement: Attribute rows render dynamically from fetched data

The `legacy-attribute-grid` SHALL render one `legacy-attribute-row` for each element in the fetched `workflowAttributes` array. Each row SHALL contain:
- A `v-select` dropdown with the `workflowName` as its label and `options` as its `:items`.
- A `span.legacy-indicator` with a color class determined by cycling through a palette: `legacy-indicator-blue`, `legacy-indicator-green`, `legacy-indicator-red`, `legacy-indicator-orange`, `legacy-indicator-purple`, `legacy-indicator-teal`.

The `v-select` `v-model` for each row SHALL bind to a reactive map keyed by `workflowName`.

#### Scenario: Three rows rendered for OrderType 0 (Printing)
- **WHEN** the order type is set to 0 and the API returns 3 workflow attributes
- **THEN** the grid renders exactly 3 `legacy-attribute-row` elements, the first labeled "印刷用紙" with 6 dropdown items, the second labeled "鋅版輸出" with 3 dropdown items, the third labeled "包裝要求" with 3 dropdown items

#### Scenario: Two rows rendered for OrderType 1 (Printed Label)
- **WHEN** the order type is set to 1 and the API returns 2 workflow attributes
- **THEN** the grid renders exactly 2 `legacy-attribute-row` elements

#### Scenario: Indicator colors cycle by index
- **WHEN** 4 or more rows are rendered
- **THEN** the 4th row uses `legacy-indicator-orange`, the 5th uses `legacy-indicator-purple`, the 6th uses `legacy-indicator-teal`, and the 7th wraps back to `legacy-indicator-blue`

---

### Requirement: Selected attribute values persist in the form draft

The `JobOrderFormData` interface SHALL include a `workflowAttributes` field of type `Record<string, string>` where keys are `workflowName` and values are the selected dropdown option. This map SHALL be initialized as an empty object and populated as the user makes selections. When the order type changes, the map SHALL be reset (cleared of previous selections).

#### Scenario: User selects a workflow attribute value
- **WHEN** the user picks "成昌" from the "印刷用紙" dropdown
- **THEN** `draft.workflowAttributes["印刷用紙"]` equals `"成昌"`

#### Scenario: Changing order type clears selections
- **WHEN** the user changes the order type dropdown
- **THEN** `draft.workflowAttributes` is reset to `{}` before new attributes are fetched

---

### Requirement: Static attribute refs and legacyAttributeOptions are removed

The following refs SHALL be removed from `JobOrderForm.vue`:
- `legacyPrintingPaper`
- `legacyFinishingOutput`
- `legacyPackagingRequirement`
- `legacyAttributeOptions` computed property

#### Scenario: No references remain
- **WHEN** searching the compiled component for `legacyPrintingPaper`, `legacyFinishingOutput`, `legacyPackagingRequirement`, or `legacyAttributeOptions`
- **THEN** no matches are found (except in git history)
