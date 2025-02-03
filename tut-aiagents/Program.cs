using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.VectorStores;

namespace tut_aiagents
{
    class Program
    {

        static void Main(string[] args)
        {
            DotNetEnv.Env.Load();
            // Create a new instance of the OpenAI connector
            var azureOpenAIConnector = CreateKernelWithChatCompletion();

        }

        static Kernel CreateKernelWithChatCompletion()
        {
            var builder = Kernel.CreateBuilder();

            AddChatCompletionToKernel(builder);

            return builder.Build();
        }

        static void AddChatCompletionToKernel(IKernelBuilder builder)
        {
            try
            {
                builder.AddAzureOpenAIChatCompletion(
                    Environment.GetEnvironmentVariable("AZURE_OPENAI_MODEL_ID"),
                    Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"),
                    Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY"));
            }
            catch (System.Exception)
            {
                throw new Exception("Please set the environment variables AZURE_OPENAI_MODEL_ID, AZURE_OPENAI_ENDPOINT, and AZURE_OPENAI_API_KEY");
            }
        }
    }
}