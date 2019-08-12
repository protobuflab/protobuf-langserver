using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using ProtobufLanguageServer.Documents;
using Protogen;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace ProtobufLanguageServer
{
    public class RenameEndpoint : IRenameHandler
    {
        private readonly ILanguageServer _router;
        private readonly ForegroundThreadManager _threadManager;
        private readonly WorkspaceSnapshotManager _snapshotManager;

        public RenameEndpoint(
            ForegroundThreadManager threadManager,
            ILanguageServer router,
            WorkspaceSnapshotManager snapshotManager)
        {
            _threadManager = threadManager;
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _snapshotManager = snapshotManager;
        }

        public RenameRegistrationOptions GetRegistrationOptions()
        {
            return new RenameRegistrationOptions()
            {
                DocumentSelector = ProtoDefaults.Selector,
            };
        }

        public async Task<WorkspaceEdit> Handle(RenameParams request, CancellationToken cancellationToken)
        {
            _router.Window.LogMessage(new LogMessageParams()
            {
                Type = MessageType.Log,
                Message = "Proto file completion list request at line: " + (request.Position.Line + 1),
            });

            _threadManager.AssertBackgroundThread();

            var document = await Task.Factory.StartNew(
                () =>
                {
                    _snapshotManager.TryResolveDocument(request.TextDocument.Uri.AbsolutePath, out var doc);
                    return doc;
                },
                CancellationToken.None,
                TaskCreationOptions.None,
                _threadManager.ForegroundScheduler);

            var syntaxTree = await document.GetSyntaxTreeAsync();
            var documentChanges = new List<TextEdit>();

            var owner = syntaxTree.Root.GetNodeAt((int)request.Position.Line, (int)request.Position.Character);


            if (owner is InputNode || owner is OutputNode || (owner is NameNode && owner.Parent is MessageNode))
            {
                var nameQuery = owner.Content;

                // We need to find all other Input, Output and Message nodes to rename them.
                var inputNodes = syntaxTree.Root.GetDescendentNodes<InputNode>();
                for (var i = 0; i < inputNodes.Count; i++)
                {
                    var inputNode = inputNodes[i];
                    if (inputNode.Content == nameQuery)
                    {
                        var textEdit = new TextEdit()
                        {
                            Range = new Range(
                                new Position(inputNode.StartLine, inputNode.StartCol),
                                new Position(inputNode.EndLine, inputNode.EndCol)),
                            NewText = request.NewName,
                        };
                        documentChanges.Add(textEdit);
                    }
                }

                var outputNodes = syntaxTree.Root.GetDescendentNodes<OutputNode>();
                for (var i = 0; i < outputNodes.Count; i++)
                {
                    var outputNode = outputNodes[i];
                    if (outputNode.Content == nameQuery)
                    {
                        var textEdit = new TextEdit()
                        {
                            Range = new Range(
                                new Position(outputNode.StartLine, outputNode.StartCol),
                                new Position(outputNode.EndLine, outputNode.EndCol)),
                            NewText = request.NewName,
                        };
                        documentChanges.Add(textEdit);
                    }
                }

                for (var i = 0; i < syntaxTree.Root.Messages.Count; i++)
                {
                    var message = syntaxTree.Root.Messages[i];
                    if (message.Name == nameQuery)
                    {
                        var textEdit = new TextEdit()
                        {
                            Range = new Range(
                                new Position(message.NameNode.StartLine, message.NameNode.StartCol),
                                new Position(message.NameNode.EndLine, message.NameNode.EndCol)),
                            NewText = request.NewName,
                        };
                        documentChanges.Add(textEdit);
                    }
                }
            }

            var workspaceEdit = new WorkspaceEdit();

            if (documentChanges.Count > 0)
            {
                workspaceEdit.Changes = new Dictionary<Uri, IEnumerable<TextEdit>>()
                {
                    [request.TextDocument.Uri] = documentChanges,
                };
            }

            return workspaceEdit;
        }

        public void SetCapability(RenameCapability capability)
        {
        }
    }
}
