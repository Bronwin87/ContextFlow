﻿using ContextFlow.Application.Request;
using ContextFlow.Application.TextUtil;
using ContextFlow.Domain;

namespace ContextFlow.Application;

public abstract class OverflowStrategy : FailStrategy<TokenOverflowException>
{
    public abstract override RequestResult ExecuteStrategy(LLMRequest request, TokenOverflowException e);
}

public class OverflowStrategySplitText : OverflowStrategy
{
    private TextSplitter Splitter;
    private TextMerger Merger;
    private string SplitAttachmentName;

    public OverflowStrategySplitText(TextSplitter splitter, TextMerger merger, string splitAttachmentName)
    {
        Splitter = splitter;
        Merger = merger;
        SplitAttachmentName = splitAttachmentName;
    }

    public override RequestResult ExecuteStrategy(LLMRequest request, TokenOverflowException e)
    {
        request.RequestConfig.Logger.Debug($"{GetType()} executing its strategy: Splitting attachment {SplitAttachmentName} and merging the outputs later on.");

        var attachment = request.Prompt.Attachments.FirstOrDefault(a => a.Name == SplitAttachmentName);
        if (attachment == null)
        {
            request.RequestConfig.Logger.Error($"Attachment with the name {SplitAttachmentName} does not exist. Unable to split it up.");
            throw new InvalidOperationException($"Attachment with the name {SplitAttachmentName} does not exist. Unable to split it up.");
        }

        var attachmentContentFragments = Splitter.Split(attachment.Content);

        request.RequestConfig.Logger.Debug("\n--- SPLIT ATTACHMENT ---\n" + String.Join("\n---\n", attachmentContentFragments) + "\n--- SPLIT ATTACHMENT ---\n");

        List<RequestResult> results = new List<RequestResult>();
        foreach ( var fragment in attachmentContentFragments )
        {
            results.Add(new LLMRequestBuilder(request)
            .UsingPrompt(request.Prompt.UsingAttachment(SplitAttachmentName, fragment))
            .Build()
            .Complete());
        }
        return new RequestResult(Merger.Merge(results.Select(r => r.RawOutput).ToList()), results[0].FinishReason);
    }
}

public class OverflowStrategyThrowException : OverflowStrategy
{
    public override RequestResult ExecuteStrategy(LLMRequest request, TokenOverflowException e)
    {
        throw e;
    }
}