
using System;
using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using ProtobufLanguageServer.Syntax;
using Protogen;

namespace ProtobufLanguageServer.Completions
{
    public class MessageKeywordCompletionItemProvider : ProtoCompletionItemProvider
    {
        private static readonly IReadOnlyList<CompletionItem> CompletionItems = new List<CompletionItem>()
        {
            new CompletionItem()
            {
                Label = "message",
                InsertText = "message ${1:Name} {$0}",
                FilterText = "message",
                SortText = "message",
                Detail = "Declare a message",
                Kind = CompletionItemKind.Keyword,
                InsertTextFormat = InsertTextFormat.Snippet,
            },
        };

        public override IReadOnlyList<CompletionItem> GetCompletionItems(Node owner, Position location, SyntaxTree syntaxTree)
        {
            if (!(owner is RootNode) && !(owner is MessageNode))
            {
                // Not top level or nested in a message node.
                return Array.Empty<CompletionItem>();
            }

            return CompletionItems;
        }
    }
}