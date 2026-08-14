# Preview status fixtures

`PreviewStatusFixture` creates five isolated repositories under the test process temporary directory:

- one main project with three configured transfer directories;
- one destination missing most configured directories;
- one destination with most directories present but stale or missing files;
- one destination whose file sizes and timestamps match the main project;
- one destination containing a file newer than the main project.

The fixture assigns fixed UTC timestamps so the status tests do not depend on checkout time or the machine clock. Every test workspace is deleted after its test completes.
