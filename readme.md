# tut-aiagents

## Overview

`tut-aiagents` is a .NET project designed as a tutorial to demonstrate the use of AI agents for various tasks. The project integrates several libraries and packages to provide a robust framework for AI-driven functionalities.

## Purpose of the Tutorial

This tutorial serves as an example to demonstrate how an AI agents system can be built using .NET and the Semantic Kernel Agent Framework, which is still in preview. It provides a hands-on approach to understanding the implementation and usage of AI agents. By following this tutorial, you will learn how to:

- Set up and configure AI agents using .NET.
- Integrate with Azure OpenAI services.
- Extend AI agent functionalities with custom plugins.
- Utilize the Semantic Kernel Agent Framework for handling semantic data and AI-driven tasks.
- Implement best practices for logging and diagnostics in AI-driven applications.

## Key Components

### Program.cs

The main entry point of the application, which includes logic for handling various types of content such as annotations, file references, images, function calls, and function results. It sets up the AI agents and defines their roles and tasks.

### Plugins.cs

Contains plugin implementations that extend the functionality of the AI agents. This includes network monitoring and host metrics functionalities.

### Dependencies

The project relies on several key packages, including but not limited to:
- `Azure.AI.OpenAI`
- `Microsoft.Extensions.Logging`
- `System.Text.Json`
- `Microsoft.SemanticKernel`
- `microsoft.semantickernel.agents.abstractions` 
- `microsoft.semantickernel.agents.core` (preview)
- `microsoft.semantickernel.agents.openai` (preview)

## Interesting Details

- **AI Integration**: Integrates with Azure's AI services, specifically the OpenAI API.
- **Logging**: Utilizes `Microsoft.Extensions.Logging` for robust logging.
- **Configuration**: Supports configuration through various `Microsoft.Extensions` packages.
- **Semantic Kernel**: Leverages the `Microsoft.SemanticKernel` for handling semantic data and AI-driven tasks.

## Getting Started

To get started with the project, follow these steps:

1. Clone the repository:
    ```sh
    git clone <repository-url>
    ```

2. Navigate to the project directory:
    ```sh
    cd tut-aiagents
    ```
   
3. Set the apikey via user-secrets
   ```
   cd tut-aiagents
   dotnet user-secrets init
   dotnet user-secrets set "AZURE_OPENAI_API_KEY" "<your-api-key>"
   dotnet user-secrets set "AZURE_OPENAI_MODEL_ID" "<your-model-id>"
   dotnet user-secrets set "AZURE_OPENAI_ENDPOINT" "<your-endpoint>"
   ```
   Alternatively you could also create a appsettings.json and set the values there. Ensure that the appsettings.json is 
   copied to the build output if you decide to take this route. Do not commit your apikey to source control.

4. Restore dependencies:
    ```sh
    dotnet restore
    ```

5. Build the project:
    ```sh
    dotnet build
    ```

6. Run the project:
    ```sh
    dotnet run
    ```

## Learning Objectives

By working through this tutorial, you will learn:
- How to set up and configure AI agents using .NET.
- How to integrate with Azure OpenAI services.
- How to extend AI agent functionalities with custom plugins.
- Best practices for logging and diagnostics in AI-driven applications.


## Contributing

Contributions are welcome! Please fork the repository and submit pull requests for any enhancements or bug fixes.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
