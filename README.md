# LiteRAG

A concise, orchestratable Retrieval-Augmented Generation (RAG) framework built with C#.

## Overview

**LiteRAG** is a lightweight and flexible Retrieval-Augmented Generation framework designed to streamline the integration of RAG patterns into .NET applications. It provides a clean, modular architecture that emphasizes orchestration and ease of use, making it ideal for building intelligent applications that combine retrieval and generation capabilities.

## Features

- **Concise Design**: Minimalist API focused on essential RAG functionality
- **Orchestratable**: Flexible pipeline orchestration for complex RAG workflows
- **Easy Integration**: Seamless integration with .NET applications
- **OpenAI Integration**: Built-in support for OpenAI models
- **Type-Safe**: Full C# type safety with modern language features
- **Async-First**: Asynchronous operations throughout for better performance

## Project Structure

LiteRAG/ 
├── Models/ # Data models and entities 
├── Orchestrations/ # Orchestration logic and workflows 
├── Processes/ # Core processing components 
└── README.md # This file


## Prerequisites

- .NET 10.0 or later
- C# 12.0 or later (for nullable reference types and latest features)

## Installation

### NuGet Package

```bash
dotnet add package Arabidopsis.LiteRAG
```

### From Source
```bash
git clone https://github.com/ArabidopsisDev/LiteRAG.git
cd LiteRAG
dotnet build
```

## Quick Start
### Basic RAG Pipeline

```csharp
using Arabidopsis.LiteRAG.Models;
using Arabidopsis.LiteRAG.Orchestrations;
using Arabidopsis.LiteRAG.Processes.Implements;

namespace Arabidopsis.LiteRAG
{
    internal class Program
    {
        public static async Task Main()
        {
            var deepSeekApi = Environment.GetEnvironmentVariable("DSAPI")!;
            var qwenApi = Environment.GetEnvironmentVariable("BitchSDAU")!;
            var src = new CancellationTokenSource();

            var ragPipeline = new LinearOrchestration<Semantics>()
                .AddChunking(new NaiveChunking("text.txt"))
                .AddClustering(new DeepSeekClustering(deepSeekApi))
                .AddEmbedding(new QwenEmbedding(qwenApi))
                .AddVectoring(new InMemoryVectoring(qwenApi, "text.txt"));

            var knowledgeBase = await ragPipeline.BuildAsync(src.Token);
        }
    }
}

```

## Dependencies

- **OpenAI** (v2.10.0): For language model integration and API access

## Architecture

### Models

Contains data structures representing RAG components such as:

- Documents and embeddings
- Retrieval results
- Generation responses
- Context and metadata

### Orchestrations

Manages the workflow and pipeline execution:

- Orchestrates retrieval and generation steps
- Handles data flow between components
- Manages execution context and state

### Processes

Core processing logic:

- Document retrieval operations
- Query processing
- Response generation
- Context enrichment

## Best Practices

1. **Batch Processing:** For better performance, batch multiple queries together
2. **Context Management:** Carefully manage context window sizes for optimal results
3. **Error Handling:** Implement proper error handling for API failures
4. **Caching:** Cache retrieval results when possible to reduce API calls
5. **Monitoring:** Log and monitor RAG pipeline performance metrics

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

### Development Setup

```bash
# Clone the repository
git clone https://github.com/ArabidopsisDev/LiteRAG.git

# Navigate to project
cd LiteRAG

# Build the project
dotnet build

# Run tests (if available)
dotnet test
```

### Guidelines

- Follow C# naming conventions (PascalCase for public members)
- Include XML documentation comments for public APIs
- Write unit tests for new features
- Ensure backward compatibility when possible

## License

This project is licensed under the **GNU Lesser General Public License v2.1 (LGPL-2.1)** - see the LICENSE file for details.
