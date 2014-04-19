﻿using System;
using System.Collections.Generic;
using System.IO;
using Sigil;

namespace Veil.Compiler
{
    internal partial class VeilTemplateCompiler
    {
        private static void EmitNode<T>(VeilCompilerState<T> state, SyntaxTreeNode rootNode)
        {
            var nodes = GetIndividualNodes(rootNode);
            foreach (var node in nodes)
            {
                var nodeType = node.GetType();
                if (nodeType == typeof(SyntaxTreeNode.WriteLiteralNode))
                    EmitWriteLiteral(state, (SyntaxTreeNode.WriteLiteralNode)node);
                else if (nodeType == typeof(SyntaxTreeNode.WriteExpressionNode))
                    EmitWriteModelProperty(state, (SyntaxTreeNode.WriteExpressionNode)node);
                else if (nodeType == typeof(SyntaxTreeNode.ConditionalOnExpressionNode))
                    EmitConditionalOnExpression(state, (SyntaxTreeNode.ConditionalOnExpressionNode)node);
                else if (nodeType == typeof(SyntaxTreeNode.EachNode))
                    EmitEach(state, (SyntaxTreeNode.EachNode)node);
                else
                    throw new VeilCompilerException("Unknown SyntaxTreeNode {0}".FormatInvariant(nodeType.Name));
            }
        }

        private static IEnumerable<SyntaxTreeNode> GetIndividualNodes(SyntaxTreeNode node)
        {
            if (node is Veil.SyntaxTreeNode.BlockNode)
                return ((Veil.SyntaxTreeNode.BlockNode)node).Nodes;
            else
                return new[] { node };
        }
    }
}