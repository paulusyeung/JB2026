## ADDED Requirements

### Requirement: GitHub Actions workflow builds and pushes to GHCR
A GitHub Actions workflow SHALL exist that builds the ClientApp Docker image and pushes it to GitHub Container Registry.

#### Scenario: Workflow triggers on push to main
- **WHEN** a push event occurs on the `main` branch
- **THEN** the workflow runs, builds the Docker image, and pushes it to GHCR

#### Scenario: Workflow triggers on push to other branches
- **WHEN** a push event occurs on a non-main branch
- **THEN** the workflow runs and builds the Docker image with branch-specific tags

#### Scenario: Workflow runs on pull requests without pushing
- **WHEN** a pull request event occurs
- **THEN** the workflow builds the Docker image to validate the build but does not push to GHCR

### Requirement: Images are tagged with commit SHA
Every pushed image SHALL be tagged with the short git commit SHA.

#### Scenario: SHA tag applied
- **WHEN** an image is pushed to GHCR
- **THEN** it has a tag matching the first 7 characters of the git commit SHA

### Requirement: Main branch images are tagged as latest
Images pushed from the `main` branch SHALL additionally be tagged with `latest`.

#### Scenario: Latest tag applied on main
- **WHEN** an image is pushed from the `main` branch
- **THEN** it has both the SHA tag and the `latest` tag

### Requirement: Images use GHCR repository path
The images SHALL be pushed to `ghcr.io` with the repository path normalized to lowercase.

#### Scenario: Correct GHCR image path
- **WHEN** an image is pushed
- **THEN** the image path follows the pattern `ghcr.io/<owner>/<repo>/clientapp` (lowercase)

### Requirement: Workflow uses Docker Buildx for layer caching
The workflow SHALL use the `docker/build-push-action` with Buildx to enable remote layer caching in GHCR.

#### Scenario: Cache is uploaded and downloaded
- **WHEN** the build step runs
- **THEN** it imports cache from GHCR and exports cache back to GHCR after the build

