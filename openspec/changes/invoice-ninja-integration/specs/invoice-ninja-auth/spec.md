"# Invoice Ninja Authentication Spec

## Overview
This capability ensures secure communication between the application backend and the Invoice Ninja API using a service-account model.

## Requirements
- **Secure Storage**: The API Key and Base URL must be stored in environment variables or a secure vault, not hardcoded.
- **Request Header**: All outgoing requests to Invoice Ninja must include the `X-Api-Token` header.
- **Error Handling**: The system must detect `401 Unauthorized` responses and log a critical alert for configuration failure.
- **Timeout**: API requests must have a defined timeout (e.g., 10 seconds) to prevent backend thread exhaustion.

## Acceptance Criteria
- [ ] Backend can successfully authenticate with a test Invoice Ninja instance.
- [ ] API keys are not exposed in any logs or frontend responses.
- [ ] Requests fail gracefully when the API key is invalid."