
using System;
using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using ProtobufLanguageServer.Syntax;
using Protogen;

namespace ProtobufLanguageServer.Completions
{
    public class TopLevelKeywordCompletionItemProvider : ProtoCompletionItemProvider
    {
        private static readonly IReadOnlyList<CompletionItem> CompletionItems = new List<CompletionItem>()
        {
            new CompletionItem()
            {
                Label = "import",
                InsertText = "import \"${1:FilePath}\";$0",
                FilterText = "import",
                SortText = "import",
                Detail = "Import another .proto definition",
                Kind = CompletionItemKind.Keyword,
                InsertTextFormat = InsertTextFormat.Snippet,
            },
            new CompletionItem()
            {
                Label = "package",
                InsertText = "package ${1:Name};$0",
                FilterText = "package",
                SortText = "package",
                Detail = "Specify a package for your proto definition",
                Kind = CompletionItemKind.Keyword,
                InsertTextFormat = InsertTextFormat.Snippet,
            },
            new CompletionItem()
            {
                Label = "option",
                InsertText = "option ${1:Name} = ${2:Value};$0",
                FilterText = "option",
                SortText = "option",
                Detail = "Specify an option for your proto definition",
                Kind = CompletionItemKind.Keyword,
                InsertTextFormat = InsertTextFormat.Snippet,
            },
        };

        public override IReadOnlyList<CompletionItem> GetCompletionItems(Node owner, Position location, SyntaxTree syntaxTree)
        {
            if (!(owner is RootNode))
            {
                // Not top level
                return Array.Empty<CompletionItem>();
            }

            return CompletionItems;
        }
    }
}