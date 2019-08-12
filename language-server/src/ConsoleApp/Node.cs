using Google.ProtocolBuffers.DescriptorProtos;
using Google.ProtocolBuffers.Descriptors;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using static Google.ProtocolBuffers.DescriptorProtos.SourceCodeInfo.Types;

namespace Protogen
{
    public class Node
    {
        public int StartLine { get; set; }
        public int StartCol { get; set; }
        public int EndLine { get; set; }
        public int EndCol { get; set; }
        public string Content { get; set; } // Temp for debugging
        public string Links { get; set; } = "";  // Temp for debugging

        public Node Parent { get; set; }
        public List<Node> Children { get; } = new List<Node>();

        public Node(Location location, SourceText text)
        {
            StartLine = location.SpanList[0];
            StartCol = location.SpanList[1];
            EndLine = location.SpanCount == 3 ? location.SpanList[0] : location.SpanList[2];
            EndCol = location.SpanCount == 3 ? location.SpanList[2] : location.SpanList[3];

            ResolveContent(text);
        }

        public IReadOnlyList<TNode> GetDescendentNodes<TNode>() where TNode : Node
        {
            var descendentNodes = new List<TNode>();

            AddDescendentNodes(this);

            return descendentNodes;

            void AddDescendentNodes(Node node)
            {
                if (node is TNode tnode)
                {
                    descendentNodes.Add(tnode);
                }

                for (var i = 0; i < node.Children.Count; i++)
                {
                    AddDescendentNodes(node.Children[i]);
                }
            }
        }

        public Node GetNodeAt(int line, int col)
        {
            var target = Children
                .SingleOrDefault(n => (n.StartLine < line || (n.StartLine == line && n.StartCol <= col)) && (line < n.EndLine || (line == n.EndLine && col <= n.EndCol)));

            if (target != null)
            {
                return target.GetNodeAt(line, col);
            }

            return this;
        }

        public void ResolveContent(SourceText text)
        {
            if (StartLine == EndLine)
            {
                Content = text.Lines[StartLine].ToString().Substring(StartCol, EndCol - StartCol);
            }
            else
            {
                var builder = new StringBuilder();
                builder.Append(text.Lines[StartLine].ToString().Substring(StartCol));
                for (int i = StartLine + 1; i < EndLine; i++)
                {
                    builder.Append(text.Lines[i]);
                }
                builder.Append(text.Lines[EndLine].ToString().Substring(0, EndCol));
                Content = builder.ToString();
            }
        }

        private static Dictionary<string, Type> DescriptorProtos = new Dictionary<string, Type>
        {
            { "type", typeof(FileDescriptorProto) }, // This doesn't look right. Should remove
            { "service", typeof(ServiceDescriptorProto) },
            { "enum", typeof(EnumDescriptorProto) },
            { "field", typeof(FieldDescriptorProto) },
            { "method", typeof(MethodDescriptorProto) },
            { "message_type", typeof(DescriptorProto) },
            { "package", null },
            { "syntax", null },
            { "name", null },
            { "input_type", null },
            { "output_type", null },
            { "label", null },
            { "number", null },
        };

        public static Node CreateNode(Location location, SourceText text)
        {
            if (location.PathList.Count == 0)
            {
                return new RootNode(location, text);
            }

            //var links = "";
            var resolvedMessageDescriptor = FileDescriptorProto.Descriptor;
            var resolvedTypeName = "";

            for (var i = 0; i < location.PathList.Count; i ++)
            {
                var path = location.PathList[i];
                //links += $" => {path}";

                if (i % 2 == 1)
                {
                    // Skip index
                    // Format of pathList is {type, index, type, index, type, index, ...} for resolving types, we don't need the index 
                    continue;
                }

                // new in proto3
                if (path == 12)
                {
                    return new SyntaxNode(location, text);
                }

                var field = resolvedMessageDescriptor.FindFieldByNumber(path);
                if (field == null)
                {
                    continue; // Should really throw here
                }

                resolvedTypeName = field.Name;
                var matchingDescriptorProto = DescriptorProtos[resolvedTypeName];

                if (matchingDescriptorProto == null)
                {
                    break;
                }

                var messageDescriptor = matchingDescriptorProto.GetProperty("Descriptor").GetValue(null);
                resolvedMessageDescriptor = (MessageDescriptor)messageDescriptor;
            }

            switch (resolvedTypeName)
            {
                case "type":
                    return new TypeNode(location, text);
                case "service":
                    return new ServiceNode(location, text);
                case "enum":
                    return new EnumNode(location, text);
                case "field":
                    return new FieldNode(location, text);
                case "method":
                    return new MethodNode(location, text);
                case "message_type":
                    return new MessageNode(location, text);
                case "package":
                    return new PackageNode(location, text);
                case "syntax":
                    return new SyntaxNode(location, text);
                case "name":
                    return new NameNode(location, text);
                case "input_type":
                    return new InputNode(location, text);
                case "output_type":
                    return new OutputNode(location, text);
                case "label":
                    // There's a bug in protobuf where labels in version 3 isn't parsed correctly
                    var node = new LabelNode(location, text);
                    node.EndLine = node.StartLine;
                    node.EndCol = text.Lines[node.StartLine].ToString().Length - 1;
                    node.ResolveContent(text);
                    return node;
                case "number":
                    return new NumberNode(location, text);
                default:
                    throw new InvalidOperationException($"Could not construct node for type {resolvedTypeName}");
            };
        }
    }
}
