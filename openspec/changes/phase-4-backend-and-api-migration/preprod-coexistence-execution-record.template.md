# Pre-Prod Coexistence Execution Record

## Change Control

- Change ticket ID: 
- Deployment approver: 
- Deployment operator: 
- Planned start (UTC): 
- Planned end (UTC): 

## Environment Confirmation

- [ ] Pre-prod URL confirmed
- [ ] Legacy JB5.API reachable from pre-prod routing layer
- [ ] Legacy JB5.REST reachable from pre-prod routing layer
- [ ] ASPNETCORE_ENVIRONMENT=Staging applied
- [ ] JWT secret injected from secret store
- [ ] CORS origins updated for pre-prod UI
- [ ] Observability exporter target configured

## Execution Log

- Artifact version / commit SHA: 
- Deployment started at (UTC): 
- Application restart completed at (UTC): 
- Health check result: 
- Verification script command used: 
- Verification output file attached: 

## Acceptance Criteria

- [ ] /api/v1/auth/token returns success
- [ ] /api/v2/auth/token returns success
- [ ] Quotation search parity verified
- [ ] Quotation range parity verified
- [ ] Jobs range parity verified
- [ ] Job orders list parity verified
- [ ] Verification JSON shows failed = 0
- [ ] No 5xx responses observed in logs during smoke window

## Rollback Record

- Rollback required: Yes / No
- If yes, rollback start (UTC): 
- If yes, rollback complete (UTC): 
- Incident / notes: 

## Sign-Off

- Technical sign-off name: 
- Product / business witness: 
- Final status: Ready for Task 3.7 / Hold
- Signed at (UTC): 
