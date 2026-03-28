## ADDED Requirements

### Requirement: Shared Library Must Provide Config, Logging, and Error Handling Extensions
The shared infrastructure library SHALL expose startup extension methods for configuration binding, Serilog structured logging, OpenTelemetry tracing, and global error handling middleware.

#### Scenario: Host project wires shared library in a single call
- **WHEN** a host project calls the shared library startup extensions in `Program.cs`
- **THEN** configuration, structured logging, tracing, and error handling SHALL all be active with no additional per-project boilerplate

### Requirement: Shared Library Must Not Reference Proprietary or License-Incompatible Packages
All dependencies in the shared infrastructure library MUST be OSS or .NET Foundation licensed and compatible with open-source redistribution.

#### Scenario: License check passes for shared library
- **WHEN** the CI license scanner runs against the shared library project
- **THEN** no incompatible dependency licenses SHALL be reported
