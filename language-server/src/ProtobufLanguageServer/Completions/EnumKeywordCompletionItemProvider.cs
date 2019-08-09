
using System;
using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using ProtobufLanguageServer.Syntax;
using Protogen;

namespace ProtobufLanguageServer.Completions
{
    public class EnumKeywordCompletionItemProvider : ProtoCompletionItemProvider
    {
        private static readonly IReadOnlyList<CompletionItem> CompletionItems = new List<CompletionItem>()
        {
            new CompletionItem()
            {
                Label = "enum",
                InsertText = "enum ${1:Name} {$0}",
                FilterText = "enum",
                SortText = "enum",
                Detail = "Declare an enum",
                Kind = CompletionItemKind.Keyword,
                InsertTextFormat = InsertTextFormat.Snippet,
            },
        };

        public override IReadOnlyList<CompletionItem> GetCompletionItems(Node owner, Position location, SyntaxTree syntaxTree)
        {
            if (!(owner is RootNode))
            {
                // Not top level or nested in a message node.
                return Array.Empty<CompletionItem>();
            }

            return CompletionItems;
        }
    }
}