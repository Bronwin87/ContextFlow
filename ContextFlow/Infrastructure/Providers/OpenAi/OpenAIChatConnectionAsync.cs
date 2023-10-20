﻿using OpenAI_API;

namespace ContextFlow.Infrastructure.Providers.OpenAI;

using ContextFlow.Domain;
using ContextFlow.Infrastructure.Logging;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using Serilog.Core;

public class OpenAIChatConnectionAsync : LLMConnectionAsync
{
    OpenAIAPI api = default!;

    public OpenAIChatConnectionAsync(string apiKey)
    {
        api = new(apiKey);
    }

    public OpenAIChatConnectionAsync()
    {
        // tries to use the environment variable OPENAI_API_KEY
        api = new();
    }

    protected override async Task<RequestResult> CallAPIAsync(string input, LLMConfig conf, CFLogger log)
    {
        try
        {
            var result = await GetChatResult(input, conf, log);
            string output = result.Choices[0].ToString();
            FinishReason finish = toCFFinishReason(result.Choices[0].FinishReason);

            return new RequestResult(output, FinishReason.Stop);
        }
        catch (Exception e)
        {
            log.Error($"Failed to get the output from the LLM. Exception: {e.GetType()}: {e.Message}");
            throw new LLMException($"Failed to get the output from the LLM. Exception: {e.GetType()}: {e.Message}");
        }
    }
}
