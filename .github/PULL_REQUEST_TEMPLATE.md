## Notes for Reviewer
<!-- Items in addition to the checklist below that the reviewer should look for -->

## Pre-Review Checklist for the PR Author

* [ ] Add sensible notes for the reviewer
* [ ] PR title is short, expressive and meaningful
* [ ] Consider switching the PR to a draft (`Convert to draft`)
    * as draft PR, the CI will be skipped for pushes
* [ ] Relevant issues are linked in the [References](#references) section
* [ ] Branch follows the naming format (`csharp-iox2-123-short-description`)
* [ ] Commits messages are according to this [guideline][commit-guidelines]
    * [ ] Commit messages have the issue ID (`[#123] Add feature description`)
* [ ] Tests have been added/updated for new functionality
* [ ] XML documentation comments added to public APIs
* [ ] Changelog updated [in the unreleased section][changelog] including API breaking changes
* [ ] Assign PR to reviewer
* [ ] All checks have passed

[commit-guidelines]: https://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html
[changelog]: https://github.com/eclipse-iceoryx/iceoryx2-csharp/blob/main/CHANGELOG.md

## PR Reviewer Reminders

* Commits are properly organized and messages are according to the guideline
* Unit tests have been written for new behavior
* Public API is documented with XML comments
* PR title describes the changes
* Code follows C# conventions (PascalCase for public APIs)
    * `dotnet format` has been exectued before submitting

## References

<!-- Use either 'Closes #123' or 'Relates to #123' to reference the corresponding issue. -->

Closes # <!-- Add issue number after '#' -->
