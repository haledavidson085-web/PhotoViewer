# Contributing

## Report an issue

Choose the appropriate structured issue form for a bug, feature request, or question. Search existing issues first and avoid uploading sensitive photos.

## Open a pull request

Keep changes focused and complete the pull-request checklist. Run a Release build before submitting:

```powershell
dotnet build --configuration Release
```

Pull requests receive labels based on the files they change. Inactive issues and pull requests receive a warning before automatic closure; add the `never-stale` label when work must remain open.

## Release notes

Apply an appropriate label such as `enhancement`, `bug`, `documentation`, `dependencies`, or `ci`. Use `skip-changelog` for changes that should not appear in generated release notes.
