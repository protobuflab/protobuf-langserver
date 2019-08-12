using Google.ProtocolBuffers.DescriptorProtos;
using Microsoft.CodeAnalysis.Text;
using Protogen;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp
{
    public class Program
    {
        [DllImport("libminiprotoc")]
        public static extern bool generate(IntPtr file, Int64 fileSize, IntPtr descriptorProtoPtr, out Int64 descriptorSize);
        
        public static async Task Main()
        {
            var stringInput = await File.ReadAllTextAsync("greet.proto");
            var text = SourceText.From(stringInput);
            var ast = GetSyntaxTree("greet.proto", text);
        }

        public static RootNode GetSyntaxTree(string path, SourceText text)
        {
            var chars = new char[text.Length];
            text.CopyTo(0, chars, 0, text.Length);
            var resolvedEncoding = text.Encoding ?? Encoding.UTF8;
            var inputFile = resolvedEncoding.GetBytes(chars);
            var nodes = new List<Node>();
            var lines = text.Lines;
            RootNode ast = null;
            using (var inputSlab = new MemoryPoolSlab(inputFile))
            using (var outputSlab = new MemoryPoolSlab(new byte[1000000]))
            {
                var parseStatus = generate(inputSlab.NativePointer, inputFile.Length, outputSlab.NativePointer, out var descriptorSize);
                var byteArray = new byte[descriptorSize];
                Marshal.Copy(outputSlab.NativePointer, byteArray, 0, (int)descriptorSize);
                var file = FileDescriptorProto.ParseFrom(byteArray);

                foreach (var location in file.SourceCodeInfo.LocationList)
                {
                    nodes.Add(Node.CreateNode(location, text));
                }

                foreach (var node in nodes.OrderByDescending(i => i.Content.Length))
                {
                    if (ast == null)
                    {
                        ast = node as RootNode;
                    }
                    else
                    {
                        var targetNode = ast.GetNodeAt(node.StartLine, node.StartCol);
                        targetNode.Children.Add(node);
                        node.Parent = targetNode;
                    }
                }
            }
            return ast;
        }
    }
}
