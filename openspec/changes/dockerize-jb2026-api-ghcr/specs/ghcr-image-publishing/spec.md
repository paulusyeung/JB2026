## ADDED Requirements

### Requirement: CI MUST publish JB2026.Api images to GHCR
The system SHALL provide a CI workflow that builds JB2026.Api container images and publishes them to GitHub Container Registry under a stable package name.

#### Scenario: Publish on eligible trigger
- **WHEN** the configured main-branch or release-tag (`v*`) trigger runs successfully
- **THEN** CI MUST push the JB2026.Api image to GHCR

#### Scenario: Build without push on pull requests
- **WHEN** CI runs on a pull request event
- **THEN** the image MUST build successfully but MUST NOT be pushed to GHCR

### Requirement: Published images MUST use traceable tags and metadata
The publishing workflow SHALL attach immutable and human-readable tags and SHALL include OCI metadata linking the image to source repository and revision.

#### Scenario: Generate required tags on main
- **WHEN** CI publishes an image for a commit on main
- **THEN** the image MUST include a short commit SHA tag (no prefix) and a rolling `latest` tag

#### Scenario: Generate branch tags on non-main branches
- **WHEN** CI publishes an image for a commit on a non-main branch
- **THEN** the image MUST include a short commit SHA tag and a sanitized branch name tag

#### Scenario: Generate release tags
- **WHEN** CI publishes an image from a release tag matching `v*`
- **THEN** the image MUST include a corresponding semantic version tag

### Requirement: Publishing MUST use least-privilege GitHub authentication
The publishing workflow SHALL use GitHub-native credentials with minimum required permissions for package publishing.

#### Scenario: Authenticate and push with workflow token
- **WHEN** the workflow executes in a repository with required package permissions enabled
- **THEN** image publishing MUST succeed without long-lived personal access tokens

### Requirement: Publishing MUST NOT affect the existing CI pipeline
The new Docker publishing workflow SHALL operate independently from the existing `ci.yml` build-test-lint pipeline.

#### Scenario: Existing CI unaffected
- **WHEN** the Docker publishing workflow is added to the repository
- **THEN** the existing `ci.yml` pipeline MUST continue to function without modification or interference

### Requirement: Publishing prerequisites MUST be documented
The repository SHALL document required permissions, settings, and troubleshooting steps for GHCR publishing.

#### Scenario: Configure repository for publishing
- **WHEN** a maintainer follows the documented prerequisites
- **THEN** they MUST be able to enable successful GHCR publication for JB2026.Api
