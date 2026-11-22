; Unshipped analyzer releases.
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MVU001 | Usage | Error | State property must be partial
MVU002 | Usage | Error | Type declaring state properties must be partial
MVU003 | Usage | Error | Invalid state property declaration
MVU004 | Usage | Error | Name reserved for generated bindings
MVU010 | Usage | Warning | Displayed member is not observable
MVU011 | Usage | Warning | Displayed list is not observable
MVU012 | Usage | Error | State property passed without its binding
MVU013 | Usage | Error | View is created but never displayed
MVU014 | Usage | Error | Statement in a view block does not produce a view
MVU015 | Usage | Warning | Switch in a view block does not handle every value
MVU016 | Usage | Error | Component is created in a view
MVU017 | Usage | Warning | Task ignores its cancellation token
