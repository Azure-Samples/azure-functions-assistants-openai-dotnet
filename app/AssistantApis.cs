using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI.Assistants;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AssistantSample;

/// <summary>
/// Defines HTTP APIs for interacting with assistants.
/// </summary>
class AssistantApis
{
    // Location to store chat history
    const string DefaultChatStorageConnectionSetting = "AzureWebJobsStorage";
    const string DefaultCollectionName = "ChatState";

    readonly ILogger<AssistantApis> logger;

    public AssistantApis(ILogger<AssistantApis> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// HTTP PUT function that creates a new assistant chat bot with the specified ID.
    /// </summary>
    [Function(nameof(CreateAssistant))]
    public CreateChatBotOutput CreateAssistant(
        [HttpTrigger(AuthorizationLevel.Function, "put", Route = "assistants/{assistantId}")]
            HttpRequestData req,
        string assistantId
    )
    {
        // Create a JSON response object with the assistant ID
        var responseJson = new { assistantId };

        // Instructions for the assistant chat bot
        string instructions = """
            Don't make assumptions about what values to plug into functions.
            Ask for clarification if a user request is ambiguous.
            """;

        // Read the request body
        using StreamReader reader = new(req.Body);

        // Return the output containing the HTTP response and the assistant creation request
        return new CreateChatBotOutput
        {
            // Set the HTTP response with the assistant ID and status code 201 (Created)
            HttpResponse = new JsonResult(responseJson) { StatusCode = 201 },

            // Create the assistant creation request with the provided instructions
            ChatBotCreateRequest = new AssistantCreateRequest(assistantId, instructions)
            {
                ChatStorageConnectionSetting = DefaultChatStorageConnectionSetting,
                CollectionName = DefaultCollectionName,
            },
        };
    }

    public class CreateChatBotOutput
    {
        // Property to hold the assistant creation request details
        [AssistantCreateOutput()]
        public AssistantCreateRequest? ChatBotCreateRequest { get; set; }

        // Property to hold the HTTP response to be returned
        [HttpResult]
        public IActionResult? HttpResponse { get; set; }
    }

    /// <summary>
    /// HTTP POST function that sends user prompts to the assistant chat bot and returns results
    /// </summary>
    [Function(nameof(PostUserQuery))]
    public IActionResult PostUserQuery(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "assistants/{assistantId}")]
            HttpRequestData req,
        string assistantId,
        [AssistantPostInput(
            "{assistantId}",
            "{message}",
            Model = "%CHAT_MODEL_DEPLOYMENT_NAME%",
            ChatStorageConnectionSetting = DefaultChatStorageConnectionSetting,
            CollectionName = DefaultCollectionName
        )]
            AssistantState state
    )
    {
        return new OkObjectResult(
            state.RecentMessages.LastOrDefault()?.Content ?? "No response returned."
        );
    }
}
