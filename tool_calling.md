# Lessons Learned: Tool Calling & Environment Execution

During the development of the Event Calendar project, several key insights were gathered regarding tool usage, environment constraints, and systematic debugging.

## 1. Environment Awareness & Exploration
- **Initial Exploration**: Before proposing a plan, it is crucial to use read-only tools (`dir`, `list_files`, `read_file`) to understand the workspace. Assuming a directory is empty or a file exists can lead to failed commands.
- **Node.js Version Constraints**: Dependency installation (specifically for Vite/Rolldown) is highly sensitive to the Node.js version. If `npm install` fails with "native binding" errors, the first step should be checking `node -v` against the package's requirements.

## 2. Shell & Command Specifics
- **PowerShell Nuances**: The `Remove-Item` command in PowerShell can be finicky with multiple paths and the `-Recurse` flag. When dealing with complex deletions (like `node_modules` and `package-lock.json`), it is safer to execute them as individual commands to avoid positional parameter errors.
- **Command Feedback**: Always interpret the full output of a command. A "Command executed" status doesn't always mean success; the error output might contain critical hints (like the `rolldown` error which pointed to a specific npm bug).

## 3. Dependency Management
- **Persistence of Errors**: When a build fails, a standard "delete and reinstall" cycle is often necessary. However, if the issue persists after a clean install, it likely indicates an environment-level incompatibility (e.g., missing build tools or incompatible Node version) rather than a local file corruption.
- **Forced Installations**: Use `npm install --force` as a last resort when standard installation fails due to peer dependency conflicts, but be aware that it can sometimes skip critical native binding links.

## 4. File Manipulation Strategy
- **Precise Matching**: When using `replace_in_file`, the `SEARCH` block must be an exact character-for-character match. Even a single missing space or newline will cause a failure.
- **Atomic Changes**: Breaking down large file modifications into multiple small `replace_in_file` blocks increases reliability and makes it easier to debug which specific change caused an issue.
- **Fallback Strategy**: If `replace_in_file` fails due to formatting issues (like line ending differences), use `read_file` to get the exact current content, then use `write_to_file` for a complete rewrite instead.

## 5. Debugging Flow
1. **Identify Error**: Read the full stack trace.
2. **Verify Environment**: Check Node/NPM versions and active processes.
3. **Isolate Problem**: Determine if the issue is in the code (syntax/types) or the environment (bindings/paths).
4. **Apply Fix**: Execute targeted commands (e.g., `npm install`, `nvm use`) before attempting to rewrite code.

## 6. Task Tracking & Progress Updates
- **task_progress Parameter**: Always include the `task_progress` parameter when using file manipulation tools (`write_to_file`, `replace_in_file`, `attempt_completion`). This keeps a checklist of completed steps and provides visibility into what has been accomplished.
- **Complete Tasks Before Completion**: Do not call `attempt_completion` until all user-requested tasks are verified as successful. Confirm each step (e.g., "file created," "line removed") before marking the overall task complete.

## 7. Tool-Specific Patterns Observed

### write_to_file Behavior
- When content is successfully saved, the tool returns confirmation with the final file content in a `<final_file_content>` block.
- The `task_progress` parameter should reflect completion status after the operation.
- Always reference the provided `final_file_content` for any future SEARCH/REPLACE operations on that file.

### replace_in_file Error Handling
- If a SEARCH block doesn't match exactly, the tool returns an error and reverts the file to its original state.
- The error message includes the exact file content at the time of failure for reference.
- When encountering repeated failures with `replace_in_file`, fall back to `write_to_file` for a complete rewrite.

### attempt_completion Flow
- This tool is used to signal task completion after all user-requested operations have succeeded.
- Provide a clear description of results in the `result` parameter.
- The `task_progress` parameter should show all checklist items marked as "DONE".
- Do not use this tool until every file operation has been confirmed successful by the environment.
