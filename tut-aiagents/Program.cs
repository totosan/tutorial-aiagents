using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using tut_aiagents.Plugins;
using Microsoft.Extensions.Logging;

#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
namespace tut_aiagents;

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder()
            .UseEnvironment("Development")
            .Build();

        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        var builder = Kernel.CreateBuilder();
        try
        {
            var modelId = configuration.GetValue<string>("AZURE_OPENAI_MODEL_ID");
            var endpoint = configuration.GetValue<string>("AZURE_OPENAI_ENDPOINT");
            var apiKey = configuration.GetValue<string>("AZURE_OPENAI_API_KEY");
            builder.AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);
        }
        catch (Exception)
        {
            throw new Exception("Please set the environment variables AZURE_OPENAI_MODEL_ID, AZURE_OPENAI_ENDPOINT, and AZURE_OPENAI_API_KEY");
        }

        var kernel = builder.Build();

        var networkKernel = kernel.Clone();
        var pluginNetwork = KernelPluginFactory.CreateFromType<NetworkMonitor>();
        networkKernel.Plugins.Add(pluginNetwork);

        var hostMetricsKernel = kernel.Clone();
        var pluginHost = KernelPluginFactory.CreateFromType<HostMetrics>();
        hostMetricsKernel.Plugins.Add(pluginHost);

        // Define the agent
        var agentAnalyst = ConfigureAgentAnalyst("Analyst", kernel, loggerFactory);
        var agentNetworkChecker = ConfigureAgentNetworkChecker("NetworkChecker", networkKernel, loggerFactory);
        var agentCommonChecker = ConfigureAgentCommonChecker("CommonChecker", hostMetricsKernel, loggerFactory);
        var agentResolver = ConfigureAgentResolver("Resolver", kernel, loggerFactory);
            
        var selectionFct = ConfigureSelectionFunction(agentAnalyst.Name, agentNetworkChecker.Name, agentCommonChecker.Name, agentResolver.Name);
        var terminationFct = ConfigureTerminationFunction();

        var groupChat = ConfigureGroupChat(agentAnalyst, agentNetworkChecker, agentResolver, agentCommonChecker, selectionFct, kernel, terminationFct, loggerFactory);


        await RunMainLoop(groupChat);
    }

    private static ChatCompletionAgent ConfigureAgentAnalyst(string nameAnalyst, Kernel kernel, ILoggerFactory loggerFactory) =>
        new()
        {
            Name = nameAnalyst,
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
                - Keep your answer alwyas short and simple.

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

    private static ChatCompletionAgent ConfigureAgentNetworkChecker(string nameNetworkChecker, Kernel networkKernel, ILoggerFactory loggerFactory) =>
        new()
        {
            Name = nameNetworkChecker,
            Instructions =
                """
                **Role:** You are a Network Checker with over 10 years of experience. You specialize in diagnosing network connectivity issues.

                **Capabilities:**

                - Check network connections and statuses.
                - Ping hosts to test if they are reachable.
                - Retrieve network adapter information.
                - Verify DNS resolutions and configurations.

                **Task:**

                - Respond to specific requests from the Analyst.
                - Provide detailed and accurate results of network checks.
                - Do not provide solutions or recommendations; only present findings.
                - DO NOT ask for additional information - just present the facts.

                **Guidelines:**

                - Ensure all information is correct and precise.
                - Present facts objectively, without opinions.
                - Organize findings clearly (use bullet points or tables if helpful).

                **Communication Style:**

                - Start your message with "I am Network Checker".
                - Use clear and direct language.
                - Be concise and focus on the requested information.
                - Maintain professionalism.
                - Keep your answer alwyas short and simple.

                **Example Response Format:**

                - **Network Status:** [Details]
                - **Ping Results:** [Details]
                - **Adapter Information:** [Details]
                - **DNS Configuration:** [Details]
                """,
            Kernel = networkKernel,
            Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
            LoggerFactory = loggerFactory
        };

    private static ChatCompletionAgent ConfigureAgentCommonChecker(string nameCommonChecker, Kernel hostMetricsKernel, ILoggerFactory loggerFactory) =>
        new()
        {
            Name = nameCommonChecker,
            Instructions =
                """
                **Role:** You are an IT generalist who checks common system issues unrelated to the network, such as hardware status, software updates, and system performance.

                **Task:**

                - When requested by the Analyst, confirm the status of non-network systems.
                - Provide brief and clear reports of system statuses.
                - You have access to CPU, Memeory and Storage information.
                - DO NOT ask for additional information - just present the facts.

                **Guidelines:**

                - Focus only on following checks
                    - CPU, Memory and Storage.

                **Rules:**

                - Unhealthy CPU is about 80% or more.
                - Unhealthy Memory is about 70% or more.
                - Unhealthy Storage is about 80% or more.

                **Communication Style:**

                - Start your message with "I am Common Checker".
                - Use clear and simple language.
                - Be concise.
                - Maintain a professional and collaborative tone.
                - Keep your answer alwyas short and simple.

                **Example Response:**

                - "All hardware components are functioning correctly."
                - "Metrics are healthy and no issues are detected."
                """,
            Kernel = hostMetricsKernel,
            Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() }),
            LoggerFactory = loggerFactory
        };

    private static ChatCompletionAgent ConfigureAgentResolver(string nameResolver, Kernel kernel, ILoggerFactory loggerFactory)
    {
        const string instructionResolver = """
                                           **Role:** You are the Resolver, responsible for providing solutions to the issues identified by the Analyst.

                                           **Task:** You first summarize the steps taken by the Analyst and the findings from the Network Checker and Common Checker. Then, you provide a clear and actionable solution to the problem.

                                           **Guidelines for two scenarios :**
                                           - first scenario:
                                               - Summarize the analysis steps and findings.
                                               - The issue is either resolved or no issue to find.
                                               - State in a seperate line **APPROVED** 
                                           - second scenario:
                                               - summarize the analysis steps and findings.
                                               - The issue is not resolved.
                                               - The analysis steps are not enough.
                                               - State in a seperate line **REJECTED**

                                           **Communication style:**
                                           - Start your message with "I am Resolver"
                                           - Keep your answer alwyas short and simple.
                                           - Be professional and clear.

                                           respond in JSON format with the following structure:
                                           ```
                                           {
                                               "approved": true,
                                               "solution": "Your solution here"
                                           }
                                           ```
                                           """;
        const string instructionResolverJson = 
            """
            ***Role:*** You are the Resolver, responsible for providing solutions to the issues identified by the Analyst.

            Respond in JSON format with the following structure:
            ```
            {
                "approved": true,
                "solution": "Your solution here"
            }
            ```
            """;
        ChatCompletionAgent agentResolver = new()
        {
            Name = nameResolver,
            Instructions = instructionResolver,
            Kernel = kernel,
            LoggerFactory = loggerFactory
        };
        return agentResolver;
    }

    private static KernelFunction ConfigureSelectionFunction(string? nameAnalyst, string? nameNetworkChecker,
        string? nameCommonChecker, string? nameResolver) =>
        AgentGroupChat.CreatePromptFunctionForStrategy(
            $$$"""
               **Task:** Determine which agent should act next based on the last response. Respond with **only** the agent's name from the list below. Do not add any explanations or extra text.

               **Agents:**

               - {{{nameAnalyst}}} 
               - {{{nameNetworkChecker}}}
               - {{{nameCommonChecker}}}
               - {{{nameResolver}}}

               **Selection Rules:**

               - **If the last response is from the user:** Choose **{{{nameAnalyst}}} **.
               - **If the last response is from {{{nameAnalyst}}}  and they are requesting network details:** Choose **{{{nameNetworkChecker}}}**.
               - **If the last response is from {{{nameAnalyst}}}  and they are requesting system checks:** Choose **{{{nameCommonChecker}}}**.
               - **If the last response is from {{{nameAnalyst}}}  and they have provided an analysis report:** Choose **{{{nameResolver}}}**.
               - **If the last response is from {{{nameNetworkChecker}}} or {{{nameCommonChecker}}}:** Choose **{{{{nameAnalyst}}} }**.
               - **Never select the same agent who provided the last response.**

               **Last Response:**

               {{$lastmessage}}
               """,
            safeParameterNames: "lastmessage"
        );

    private static KernelFunction ConfigureTerminationFunction() =>
        AgentGroupChat.CreatePromptFunctionForStrategy(
            $$$"""
               **Task:** Determine, if the 'Resolver' approved or rejected.

               **Rules:**
               - you can end this conversation, if 'Resolver' approved.
               - you can only approve, if last message was from 'Resolver'.
               - **If the Resolver has approved:** Respond with **YES**.
               - **If there are still unresolved issues or action is required:** Respond with **NO**.

               **Communication**:
               Provide a simple and short eplanation about your decission.
               Provide a last single line with only **YES** or **NO**. Do not add any additional text to that line.

               **Last Response:**

               {{$lastmessage}}
               """,
            safeParameterNames: "lastmessage"
        );

    private static AgentGroupChat ConfigureGroupChat(ChatCompletionAgent agentAnalyst,
        ChatCompletionAgent agentNetworkChecker, ChatCompletionAgent agentResolver, ChatCompletionAgent agentCommonChecker,
        KernelFunction selectionFct, Kernel kernel, KernelFunction terminationFct, ILoggerFactory loggerFactory)
    {
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
                    //Agents = new[] { agentAnalyst, agentNetworkChecker, agentResolver, agentCommonChecker },
                    Agents = new[] {  agentResolver },
                    HistoryVariableName = "lastmessage",
                    HistoryReducer = new ChatHistoryTruncationReducer(1),
                    MaximumIterations = 10,
                    ResultParser = (recall) => {
                        var result =recall.GetValue<string>();
                        var yes = result?.ToLower().Contains("yes");
                        if((bool)yes) 
                        {
                            Console.WriteLine($"Termination: {result}");
                        }
                        return yes ?? false;}
                            
                }
            },
            LoggerFactory = loggerFactory,
        };
        return groupChat;
    }

    private static async Task RunMainLoop(AgentGroupChat groupChat)
    {
        while(true)
        {
            Console.WriteLine();
            Console.Write("User > ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }
            input = input.Trim();
            if (input.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
            {
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
                await foreach (var response in groupChat.InvokeAsync())
                {
                    Console.WriteLine();
                    Console.WriteLine($"{response.AuthorName?.ToUpperInvariant()}:{Environment.NewLine}{response.Content}");
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
        }
    }


    private sealed class ApprovalTerminationStrategy : TerminationStrategy
    {
        // Terminate when the final message contains the term "approve"
        protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
            => Task.FromResult(history[history.Count - 1].Content?.Contains("approve", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    static void WriteAgentChatMessage(ChatMessageContent message)
    {
        // Include ChatMessageContent.AuthorName in output, if present.
        string authorExpression = message.Role == AuthorRole.User ? string.Empty : $" - {message.AuthorName ?? "*"}";
        // Include TextContent (via ChatMessageContent.Content), if present.
        string contentExpression = string.IsNullOrWhiteSpace(message.Content) ? string.Empty : message.Content;
        bool isCode = message.Metadata?.ContainsKey(OpenAIAssistantAgent.CodeInterpreterMetadataKey) ?? false;
        string codeMarker = isCode ? "\n  [CODE]\n" : " ";
        Console.WriteLine($"\n# {message.Role}{authorExpression}:{codeMarker}{contentExpression}");

        // Provide visibility for inner content (that isn't TextContent).
        foreach (KernelContent item in message.Items)
        {
            if (item is AnnotationContent annotation)
            {
                Console.WriteLine($"  [{item.GetType().Name}] {annotation.Quote}: File #{annotation.FileId}");
            }
            else if (item is FileReferenceContent fileReference)
            {
                Console.WriteLine($"  [{item.GetType().Name}] File #{fileReference.FileId}");
            }
            else if (item is ImageContent image)
            {
                Console.WriteLine($"  [{item.GetType().Name}] {image.Uri?.ToString() ?? image.DataUri ?? $"{image.Data?.Length} bytes"}");
            }
            else if (item is FunctionCallContent functionCall)
            {
                Console.WriteLine($"  [{item.GetType().Name}] {functionCall.Id}");
            }
            else if (item is FunctionResultContent functionResult)
            {
                Console.WriteLine($"  [{item.GetType().Name}] {functionResult.CallId} - {functionResult.Result?.ToString() ?? "*"}");
            }
        }



        void WriteUsage(long totalTokens, long inputTokens, long outputTokens)
        {
            Console.WriteLine($"  [Usage] Tokens: {totalTokens}, Input: {inputTokens}, Output: {outputTokens}");
        }
    }
}
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.