# GitHub Workflows

## build.yml

This workflow builds the iceoryx2-csharp project and creates NuGet packages.

### Workflow Steps

1. **Build Native Library** (Multi-platform)
   * Runs on: Ubuntu, macOS, and Windows
   * Checks out the repository with submodules
   * Sets up Rust toolchain
   * Builds the iceoryx2 C FFI library in release mode
   * Uploads native libraries as artifacts for each platform

2. **Build .NET Library**
   * Downloads all native libraries from the previous step
   * Sets up .NET 8.0 and 9.0
   * Restores dependencies
   * Builds the solution in Release configuration
   * Runs tests
   * Uploads build artifacts

3. **Create NuGet Packages**
   * Downloads native libraries
   * Creates NuGet packages for:
     * iceoryx2 (main library)
     * iceoryx2.Reactive (reactive extensions)
   * Packages include native libraries for all platforms
   * Uploads packages as artifacts

4. **Publish to NuGet** (only on tags)
   * Automatically publishes packages when a tag is pushed
   * Requires `NUGET_API_KEY` secret to be configured

### Triggers

* **Push** to `main` branch
* **Pull requests** to `main` branch
* **Manual trigger** via workflow_dispatch

### Secrets Required

For automatic publishing to NuGet.org:

* `NUGET_API_KEY`: Your NuGet.org API key

### Artifacts

The workflow produces the following artifacts:

* `native-linux-x64`: Linux native library
* `native-osx-x64`: macOS native library
* `native-win-x64`: Windows native library
* `dotnet-build`: Compiled .NET assemblies
* `nuget-packages`: NuGet package files (.nupkg)

### Usage

The workflow runs automatically on push and pull requests. To manually trigger:

1. Go to the "Actions" tab in GitHub
2. Select "Build and Package" workflow
3. Click "Run workflow"
4. Select the branch and click "Run workflow"

### Publishing a Release

To publish a new version to NuGet.org:

1. Update version numbers in:
   * `src/Iceoryx2/Iceoryx2.csproj`
   * `src/Iceoryx2.Reactive/Iceoryx2.Reactive.csproj`

2. Commit and push changes

3. Create and push a tag:
   ```bash
   git tag v0.7.0
   git push origin v0.7.0
   ```

4. The workflow will automatically build and publish to NuGet.org
