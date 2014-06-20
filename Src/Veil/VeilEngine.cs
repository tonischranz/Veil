﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Veil.Compiler;
using Veil.Parser;

namespace Veil
{
    /// <summary>
    /// Compiles templates for execution
    /// </summary>
    public class VeilEngine : IVeilEngine
    {
        private static readonly MethodInfo compileMethod = typeof(VeilEngine).GetMethod("Compile");
        private static IDictionary<string, Func<ITemplateParser>> parserFactories = new Dictionary<string, Func<ITemplateParser>>();
        private readonly IVeilContext context;

        /// <summary>
        /// Creates a VeilEngine that does not support Includes/Partials/MasterPages.
        /// </summary>
        public VeilEngine()
            : this(new NullVeilContext())
        {
        }

        /// <summary>
        /// Creates a VeilEngine with an <see cref="IVeilContext"/> to enable support for Includes/Partials/MasterPages.
        /// </summary>
        public VeilEngine(IVeilContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Parses and compiles the specified template
        /// </summary>
        /// <typeparam name="T">The type of the model that will be passed to the template</typeparam>
        /// <param name="templateType">Name of the <see cref="ITemplateParser"/> to use to parse the template. See <see cref="VeilEngine.RegisterParser"/></param>
        /// <param name="templateContents">The contents of the template to compile</param>
        /// <returns>A compiled action ready to be executed as needed to render the template</returns>
        public Action<TextWriter, T> Compile<T>(string templateType, TextReader templateContents)
        {
            if (String.IsNullOrEmpty(templateType)) throw new ArgumentNullException("templateType");
            if (templateContents == null) throw new ArgumentNullException("templateContents");
            if (!parserFactories.ContainsKey(templateType)) throw new ArgumentException("A parser for templateType '{0}' is not registered.".FormatInvariant(templateType), "templateType");

            var syntaxTree = parserFactories[templateType]().Parse(templateContents, typeof(T));
            return new VeilTemplateCompiler<T>(CreateIncludeParser(templateType, context)).Compile(syntaxTree);
        }

        /// <summary>
        /// Parses and compiles the specified template when the model type is not known
        /// </summary>
        /// <param name="templateType">Name of the <see cref="ITemplateParser"/> to use to parse the template. See <see cref="VeilEngine.RegisterParser"/></param>
        /// <param name="templateContents">The contents of the template to compile</param>
        /// <param name="modelType">The type of the model that will be passed to the template</param>
        /// <returns>A compiled action that will cast the model before execution</returns>
        public Action<TextWriter, object> CompileNonGeneric(string templateType, TextReader templateContents, Type modelType)
        {
            var typedCompileMethod = compileMethod.MakeGenericMethod(modelType);
            var compiledTemplate = typedCompileMethod.Invoke(this, new object[] { templateType, templateContents });
            var compiledTemplateType = compiledTemplate.GetType();

            // TODO: Optimize this without casting from object? Perhaps using Expressions
            var emitter = Emit<Action<TextWriter, object, object>>.NewDynamicMethod();
            emitter.LoadArgument(2);
            emitter.CastClass(compiledTemplateType);
            emitter.LoadArgument(0);
            emitter.LoadArgument(1);
            emitter.CastClass(modelType);
            emitter.Call(compiledTemplateType.GetMethod("Invoke"));
            emitter.Return();
            var del = emitter.CreateDelegate();
            return new Action<TextWriter, object>((w, m) => del(w, m, compiledTemplate));
        }

        /// <summary>
        /// Registers a parser instance for use by the engine
        /// </summary>
        /// <param name="templateType">The key that will be used to signal the engine to use this parser. See <see cref="VeilEngine.Compile"/></param>
        /// <param name="parser">An instance of the parser that will be reused for each compile</param>
        public static void RegisterParser(string templateType, ITemplateParser parser)
        {
            RegisterParser(templateType, () => parser);
        }

        /// <summary>
        /// Registers a parser factory for use by the engine
        /// </summary>
        /// <param name="templateType">The key that will be used to signal the engine to use this parser. See <see cref="VeilEngine.Compile"/></param>
        /// <param name="parserFactory">A factory for the parser. The factory is invoked once for each compile</param>
        public static void RegisterParser(string templateType, Func<ITemplateParser> parserFactory)
        {
            if (String.IsNullOrEmpty(templateType)) throw new ArgumentNullException("templateType");
            if (parserFactories.ContainsKey(templateType)) throw new ArgumentException("A parser for templateType '{0}' ({1}) is already registered.".FormatInvariant(templateType, parserFactories[templateType].GetType().Name), "templateType");

            parserFactories.Add(templateType, parserFactory);
        }

        /// <summary>
        /// Clear all currently registered parsers
        /// </summary>
        public static void ClearParserRegistrations()
        {
            parserFactories.Clear();
        }

        private static Func<string, Type, SyntaxTreeNode> CreateIncludeParser(string templateType, IVeilContext context)
        {
            return (includeName, modelType) =>
            {
                var template = context.GetTemplateByName(includeName, templateType);
                if (template == null) throw new InvalidOperationException("Unable to load template '{0}' using parser '{1}'".FormatInvariant(includeName, templateType));
                return parserFactories[templateType]().Parse(template, modelType);
            };
        }
    }
}