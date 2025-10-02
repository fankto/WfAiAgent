# Contributing to Workflow+ AI Agent

First off, thank you for considering contributing to Workflow+ AI Agent! It's people like you that make this project better.

## Code of Conduct

This project and everyone participating in it is governed by our Code of Conduct. By participating, you are expected to uphold this code.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the existing issues as you might find out that you don't need to create one. When you are creating a bug report, please include as many details as possible:

* Use a clear and descriptive title
* Describe the exact steps which reproduce the problem
* Provide specific examples to demonstrate the steps
* Describe the behavior you observed after following the steps
* Explain which behavior you expected to see instead and why
* Include screenshots if possible
* Include your environment details (OS, .NET version, etc.)

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, please include:

* Use a clear and descriptive title
* Provide a step-by-step description of the suggested enhancement
* Provide specific examples to demonstrate the steps
* Describe the current behavior and explain which behavior you expected to see instead
* Explain why this enhancement would be useful

### Pull Requests

* Fill in the required template
* Do not include issue numbers in the PR title
* Follow the C# coding style
* Include thoughtfully-worded, well-structured tests
* Document new code
* End all files with a newline

## Development Process

### Setting Up Your Development Environment

1. Fork the repo
2. Clone your fork
```bash
git clone https://github.com/YOUR_USERNAME/WfAiAgent.git
cd WfAiAgent
```

3. Add upstream remote
```bash
git remote add upstream https://github.com/fankto/WfAiAgent.git
```

4. Create a branch
```bash
git checkout -b feature/my-feature
```

### Building the Project

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Code Style

* Follow standard C# conventions
* Use meaningful variable and method names
* Add XML documentation comments for public APIs
* Keep methods focused and small
* Write unit tests for new functionality

### Commit Messages

* Use the present tense ("Add feature" not "Added feature")
* Use the imperative mood ("Move cursor to..." not "Moves cursor to...")
* Limit the first line to 72 characters or less
* Reference issues and pull requests liberally after the first line

Example:
```
Add dark mode toggle to settings

- Implement theme switching logic
- Add persistence to localStorage
- Update UI components for dark theme

Fixes #123
```

### Submitting Changes

1. Push to your fork
```bash
git push origin feature/my-feature
```

2. Create a Pull Request from your fork to the main repository
3. Wait for review and address any feedback

## Project Structure

```
WfAiAgent/
├── src/
│   ├── Agent/              # Main agent implementation
│   ├── Core/               # Core abstractions
│   └── Observability/      # Logging and metrics
├── tests/                  # Tests
└── docs/                   # Documentation
```

## Testing Guidelines

* Write unit tests for all new functionality
* Ensure all tests pass before submitting PR
* Aim for high code coverage
* Test edge cases and error conditions

## Documentation

* Update README.md if needed
* Add XML comments to public APIs
* Update relevant documentation files
* Include code examples where appropriate

## Questions?

Feel free to open an issue with your question or reach out to the maintainers.

## Recognition

Contributors will be recognized in the project README and release notes.

Thank you for contributing! 🎉
