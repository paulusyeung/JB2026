## ADDED Requirements

### Requirement: Multi-stage Dockerfile exists
The ClientApp directory SHALL contain a `Dockerfile` that uses a multi-stage build: a Node.js build stage followed by an nginx runtime stage.

#### Scenario: Dockerfile has two stages
- **WHEN** the Dockerfile is parsed
- **THEN** it contains at least two `FROM` instructions, the first using a Node.js base image and the second using an nginx base image

### Requirement: Build stage produces production assets
The build stage SHALL install dependencies and run the Vite production build, producing static assets.

#### Scenario: Dependencies installed and build succeeds
- **WHEN** the build stage runs
- **THEN** `package.json` and lock files are copied, dependencies are installed, and `vite build` completes successfully

#### Scenario: Build output is available for the runtime stage
- **WHEN** the build stage completes
- **THEN** the built assets exist in the expected output directory (`wwwroot/app` relative to the ClientApp directory)

### Requirement: Runtime stage serves static files via nginx
The runtime stage SHALL copy the built assets and configure nginx to serve them.

#### Scenario: Nginx serves the SPA on port 80
- **WHEN** the container starts
- **THEN** nginx is listening on port 80 and serves the built static files

#### Scenario: SPA index.html is served for client-side routing
- **WHEN** a request is made to any path under `/app/`
- **THEN** nginx returns the correct static file or falls back to `index.html` for client-side routing

### Requirement: Dependency layer caching is optimized
The Dockerfile SHALL copy `package.json` and lock files before source code to maximize Docker layer cache hits.

#### Scenario: Dependency install layer is cached when source changes
- **WHEN** only source files change (not `package.json` or lock files)
- **THEN** the dependency installation layer is reused from cache

### Requirement: Runtime image runs as non-root user
The nginx process in the runtime stage SHALL run as a non-root user.

#### Scenario: Nginx worker runs as non-root
- **WHEN** the container is running
- **THEN** the nginx worker processes are not running as UID 0

### Requirement: Dockerignore excludes unnecessary files
A `.dockerignore` file SHALL exist in the ClientApp directory to exclude `node_modules`, test files, and other unnecessary content from the build context.

#### Scenario: Node modules excluded from context
- **WHEN** the Docker build context is assembled
- **THEN** `node_modules/` is not included in the context

#### Scenario: Test files excluded from context
- **WHEN** the Docker build context is assembled
- **THEN** `tests/` and `test-results/` are not included in the context

