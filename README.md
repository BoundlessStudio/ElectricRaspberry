
# Electric Raspberry
Introducing Electric Raspberry: a robust and efficient C# minimal API backend, designed for high-performance web applications.

---

## Table of Contents
- [Introduction](#introduction)
- [Architecture](#architecture)
- [Features](#features)
- [Installation](#installation)
- [Usage](#usage)
- [Documentation](#documentation)
- [Support and Feedback](#support-and-feedback)
- [License](#license)
- [Acknowledgements](#acknowledgements)

---

# Architecture

## Orchestration

The process flow of a query orchestration system involving an a browser, server and ChatGPT.

```mermaid
flowchart TD
%%{init: { 'theme': 'neutral' } }%%
  A[User sends query] --> B[Server forwards request to ChatGPT]
  B --> C[ChatGPT responds with answer]
  C --> D{Finish Reason?}
  D --> E[Stop] --> F[Server sends answer to User]
  D --> G[Length]
  G --> H[Server sends message to continue]
  H --> B
  D --> I[Tool Calls]
  I --> J[Server executes tool call]
  J --> B
```

## Chat History

The Chat history is updated via the following events.

```mermaid
timeline
%%{init: { 'theme': 'neutral' } }%%
  User sends query to Server: User query added
  Length: Partial answer added
  Tool Calls: Call Tools
  : Results added
  Stop: Answer added
```