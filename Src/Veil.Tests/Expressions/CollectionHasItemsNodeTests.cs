﻿using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Veil.Expressions
{
    [TestFixture]
    internal class CollectionHasItemsNodeTests
    {
        [TestCaseSource("InvalidCases")]
        public void Should_throw_when_collection_expression_not_suitable<T>(T model)
        {
            Assert.Throws<VeilParserException>(() =>
            {
                SyntaxTreeNode.ExpressionNode.HasItems(SyntaxTreeNode.ExpressionNode.Property(model.GetType(), "Items"));
            });
        }

        [TestCaseSource("ValidCases")]
        public void Should_not_throw_when_collection_expression_is_a_suitable_type<T>(T model)
        {
            Assert.DoesNotThrow(() =>
            {
                SyntaxTreeNode.ExpressionNode.HasItems(SyntaxTreeNode.ExpressionNode.Property(model.GetType(), "Items"));
            });
        }

        public object[] ValidCases()
        {
            return new object[] {
                new object[] { new { Items = new ArrayList() } },
                new object[] { new { Items = new List<string>() } },
                new object[] { new { Items = new Dictionary<string, string>() } },
                new object[] { new { Items = new [] { "" } } }
            };
        }

        public object[] InvalidCases()
        {
            return new object[] {
                new object[] { new { Items = "" } },
                new object[] { new { Items = 1 } },
                new object[] { new { Items = false } },
                new object[] { new { Items = DateTime.Now } },
                new object[] { new { Items = new object() } }
            };
        }
    }
}