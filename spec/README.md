# Feature specifications

Large features are specified here before they are implemented, in the spirit of the
[C# language proposals](https://github.com/dotnet/csharplang/tree/main/proposals). A specification is short, shows
code, and is updated as the feature moves on.

| Feature | Status |
|---|---|
| [View builder blocks](view-builder.md) | Accepted |
| [View lifecycle](lifecycle.md) | Accepted |

## Format

One file per feature, named after it (`view-builder.md`), with these sections:

- **Summary**: the feature in two sentences and one code sample.
- **Motivation**: what is hard today, with the current code.
- **Detailed design**: the API, how it works, the diagnostics.
- **Strengths**, **Weaknesses** and **Risks**: an honest assessment.
- **Alternatives**: what else was considered, and why it was not chosen.
- **Unresolved questions**: the decisions still open.
- **Spike results**, when a prototype verified the risky parts.

Statuses: *Proposed*, *Accepted*, *Implemented*, *Rejected*.
