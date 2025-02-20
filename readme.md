# tut-aiagents

## Overview

`tut-aiagents` is a .NET project designed as a tutorial to demonstrate the use of AI agents for various tasks. The project integrates several libraries and packages to provide a robust framework for AI-driven functionalities.

## Project Structure

The repository is organized as follows:

```
.editorconfig
.gitignore
.vscode/
    launch.json
    tasks.json
tut-aiagents/
    .env
    bin/
        Debug/
            net9.0/
                Azure.AI.OpenAI.dll
                Azure.Core.dll
                DotNetEnv.dll
                Microsoft.Bcl.AsyncInterfaces.dll
                Microsoft.Bcl.HashCode.dll
                ...
    obj/
        Debug/
            net9.0/
        project.assets.json
        project.nuget.cache
        tut-aiagents.csproj.nuget.dgspec.json
        tut-aiagents.csproj.nuget.g.props
        tut-aiagents.csproj.nuget.g.targets
    Plugins.cs
    Program.cs
    readme.md
    tut-aiagents.csproj
tut-aiagents.sln
```

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

These dependencies are managed via NuGet and are specified in the `project.assets.json` file.

## Interesting Details

- **AI Integration**: The project integrates with Azure's AI services, specifically the OpenAI API, to provide advanced AI capabilities.
- **Logging**: Utilizes `Microsoft.Extensions.Logging` for robust logging and diagnostics.
- **Configuration**: Supports configuration through various `Microsoft.Extensions` packages, ensuring flexibility and ease of use.
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

3. Restore dependencies:
    ```sh
    dotnet restore
    ```

4. Build the project:
    ```sh
    dotnet build
    ```

5. Run the project:
    ```sh
    dotnet run
    ```

## Learning Objectives

By working through this tutorial, you will learn:
- How to set up and configure AI agents using .NET.
- How to integrate with Azure OpenAI services.
- How to extend AI agent functionalities with custom plugins.
- Best practices for logging and diagnostics in AI-driven applications.

## Creating Your Own AI Agent Project

To create your own AI agent project, follow these steps:

1. **Create a new .NET project:**
    ```sh
    dotnet new console -n MyAIAgentProject
    cd MyAIAgentProject
    ```

2. **Add necessary NuGet packages:**
    ```sh
    dotnet add package DotNetEnv --version 3.1.1
    dotnet add package Microsoft.Extensions.Logging.Console --version 9.0.1
    dotnet add package Microsoft.SemanticKernel --version 1.35.0
    dotnet add package microsoft.semantickernel.agents.abstractions --version 1.35.0-alpha
    dotnet add package microsoft.semantickernel.agents.core --version 1.35.0-alpha
    dotnet add package microsoft.semantickernel.agents.openai --version 1.35.0-alpha
    dotnet add package System.Diagnostics.PerformanceCounter --version 9.0.1
    ```

3. **Add necessary `using` statements in `Program.cs`:**
    ```csharp
    using Microsoft.SemanticKernel;
    using Microsoft.SemanticKernel.Agents;
    using Microsoft.SemanticKernel.Agents.Chat;
    using Microsoft.SemanticKernel.Agents.OpenAI;
    using Microsoft.Extensions.Logging;
    using DotNetEnv;
    ```

4. **Set up environment variables for Azure OpenAI:**
    Create a `.env` file in the project root with the following content:
    ```
    AZURE_OPENAI_MODEL_ID=<your-model-id>
    AZURE_OPENAI_ENDPOINT=<your-endpoint>
    AZURE_OPENAI_API_KEY=<your-api-key>
    ```

5. **Initialize the kernel and agents in `Program.cs`:**
    ```csharp
    var builder = Kernel.CreateBuilder();
    var envs = DotNetEnv.Env.Load();
    builder.AddAzureOpenAIChatCompletion(
        Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL_ID"),
        Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"),
        Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"));
    var kernel = builder.Build();

    var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    var logger = loggerFactory.CreateLogger<Program>();
    ```

6. **Define your agents and their roles:**
    ```csharp
    ChatCompletionAgent agent = new()
    {
        Name = "MyAgent",
        Instructions = "Your agent instructions here...",
        Kernel = kernel,
        LoggerFactory = loggerFactory
    };
    ```

## Example: Creating and Using Agents

### Step 1: Define the Agents

In `Program.cs`, define the agents with their roles and instructions. For example, an Analyst agent:

```csharp
ChatCompletionAgent agentAnalyst = new()
{
    Name = "Analyst",
    Instructions =
    """
    **Role:** You are an experienced IT professional specializing in network troubleshooting. You have extensive knowledge of network connections, security, and general IT issues.

    **Task:**

    - When a user reports a network-related problem (e.g., "My video is not playing smoothly"), your job is to identify potential host system issues causing the problem.
    - Relevant information are Network, CPU, RAM, and Memory.
    - Create a clear and simple step-by-step plan to diagnose the issue, starting with the most likely causes.
    - Request specific information from the Network Checker or the Common Checker as needed.
    - After asking your team for facts, summarize your findings and pass them to the Resolver for solutions.
    - When no issue is found, state that clearly. It is ok to end the communication with the Resolver.

    **Guidelines:**

    - Be methodical and systematic in your approach.
    - Communicate clearly and specifically when asking for information.
    - Do not provide solutions; focus only on analysis.
    - Keep records of your analysis steps and findings.

    **Communication Style:**

    - Use simple and clear language.
    - Avoid unnecessary technical jargon.
    - Be professional and collaborative.
    - Keep your answer always short and simple.

    **Example Workflow:**

    1. Receive the user's issue.
    2. Identify the most probable network-related causes.
    3. Ask the Network Checker for specific network details.
    4. Analyze the information received.
    5. If needed, ask the Common Checker about other system statuses.
    6. Summarize the analysis.
    7. Send the analysis report to the Resolver.
    """,
    Kernel = kernel,
    LoggerFactory = loggerFactory
};
```

### Step 2: Create an Agent Group

Create an agent group to manage interactions between multiple agents:

```csharp
AgentGroupChat groupChat = new(agentAnalyst, agentNetworkChecker, agentResolver, agentCommonChecker)
{
    ExecutionSettings = new()
    {
        SelectionStrategy = new KernelFunctionSelectionStrategy(selectionFct, kernel)
        {
            InitialAgent = agentAnalyst,
            HistoryReducer = new ChatHistoryTruncationReducer(1),
            HistoryVariableName = "lastmessage",
            ResultParser = (result) =>
            {
                var selection = result.GetValue<string>() ?? agentAnalyst.Name;
                Console.WriteLine($"Next agent: {selection}");
                return selection;
            }
        },
        TerminationStrategy = new KernelFunctionTerminationStrategy(terminationFct, kernel)
        {
            Agents = new[] { agentAnalyst, agentNetworkChecker, agentResolver, agentCommonChecker },
            HistoryVariableName = "lastmessage",
            HistoryReducer = new ChatHistoryTruncationReducer(1),
            MaximumIterations = 10,
            ResultParser = (recall) => {
                var result = recall.GetValue<string>();
                var yes = result?.ToLower().Contains("yes");
                if ((bool)yes)
                {
                    Console.WriteLine($"Termination: {result}");
                }
                return yes ?? false;
            }
        }
    },
    LoggerFactory = loggerFactory,
};
```

### Step 3: Interact with the Agent Group

Add logic to interact with the agent group:

```csharp
bool isComplete = false;
do
{
    Console.WriteLine();
    Console.Write("User > ");
    string input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }
    input = input.Trim();
    if (input.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
    {
        isComplete = true;
        break;
    }

    if (input.Equals("RESET", StringComparison.OrdinalIgnoreCase))
    {
        await groupChat.ResetAsync();
        Console.WriteLine("[Conversation has been reset]");
        continue;
    }

    groupChat.AddChatMessage(new ChatMessageContent(AuthorRole.User, input));
    groupChat.IsComplete = false;

    try
    {
        await foreach (ChatMessageContent response in groupChat.InvokeAsync())
        {
            Console.WriteLine();
            Console.WriteLine($"{response.AuthorName.ToUpperInvariant()}:{Environment.NewLine}{response.Content}");
        }
    }
    catch (HttpOperationException exception)
    {
        Console.WriteLine(exception.Message);
        if (exception.InnerException != null)
        {
            Console.WriteLine(exception.InnerException.Message);
            if (exception.InnerException.Data.Count > 0)
            {
                Console.WriteLine(JsonSerializer.Serialize(exception.InnerException.Data, new JsonSerializerOptions() { WriteIndented = true }));
            }
        }
    }
} while (!isComplete);
```

## Contributing

Contributions are welcome! Please fork the repository and submit pull requests for any enhancements or bug fixes.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
