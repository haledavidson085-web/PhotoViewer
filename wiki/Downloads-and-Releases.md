# Downloads and releases

Every successful `Build and publish` workflow run uploads four ZIP packages for 14 days:

- Windows x64 framework-dependent
- Windows x64 self-contained
- Windows ARM64 framework-dependent
- Windows ARM64 self-contained

Version tags matching `v*`, such as `v1.0.0`, create a permanent GitHub Release. Releases include all four packages, SHA-256 checksums, and automatically categorized release notes.

Use the self-contained package for the simplest installation. Use the framework-dependent package when the .NET 10 Desktop Runtime is already installed and download size matters.
