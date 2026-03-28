## ADDED Requirements

### Requirement: All Legacy Stored Procedure Calls Must Be Re-Implemented for EF Core 8
Every stored procedure previously called via EF6 function imports MUST be re-implemented using `FromSqlRaw`, `ExecuteSqlRaw`, or a `DbCommand` approach in EF Core 8.

#### Scenario: Stored procedure produces identical output to EF6 equivalent
- **WHEN** a stored procedure is called via the new EF Core implementation
- **THEN** the result SHALL match the output of the legacy EF6 function import call

### Requirement: Stored Procedure Calls Must Use Parameterised Inputs
All stored procedure calls MUST use parameterised inputs. String concatenation into SQL MUST NOT be used.

#### Scenario: Stored procedure call is parameterised
- **WHEN** the stored procedure call implementation is reviewed
- **THEN** all input values SHALL be passed as named or positional parameters, not as concatenated strings
