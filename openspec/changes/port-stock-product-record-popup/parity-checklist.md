# ProductRecord Parity Checklist

## Fields

- Customer code segment captured and required in dialog.
- Category code segment captured and required in dialog.
- Sequence number segment captured and required in dialog.
- Composed stock number displayed as read-only derived field.
- Product code required and uniqueness-validated.
- Product name required.
- Production info/description editable.
- Remarks editable.
- Selling price editable.
- COGS editable.
- Balance read-only.

## Behaviors

- Create mode opens from NEW PRODUCT and starts with blank fields.
- Edit mode opens from stock row/card click with existing record loaded.
- Save and Save and Close supported with confirmation prompts.
- Delete supported in edit mode with confirmation.
- Create save transitions dialog to edit mode for new record.
- Movement history shown in edit mode with running balance.
- Non-goal actions (Attachment, Stock In/Out, Print, Export) shown as gated placeholders.

## Validation

- Required fields: customer code, category code, sequence number, product code, product name.
- Product code uniqueness enforced for create and changed edit code.
- Next-number flow available from customer/category context.
