using System.Collections.Generic;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using ProtobufLanguageServer.Syntax;
using Protogen;

namespace ProtobufLanguageServer.Completions
{
    public abstract class ProtoCompletionItemProvider
    {
        public abstract IReadOnlyList<CompletionItem> GetCompletionItems(Node owner, Position location, SyntaxTree syntaxTree);
    }
}