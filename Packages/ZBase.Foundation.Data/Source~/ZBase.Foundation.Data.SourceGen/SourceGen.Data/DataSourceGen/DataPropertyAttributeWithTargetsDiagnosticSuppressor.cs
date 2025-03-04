﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ZBase.Foundation.SourceGen;

using static ZBase.Foundation.Data.SuppressionDescriptors;

namespace ZBase.Foundation.Data.ObservablePropertySourceGen
{
    /// <summary>
    /// <para>
    /// A diagnostic suppressor to suppress CS0657 warnings for properties with [DataProperty] using a [field:] attribute list.
    /// </para>
    /// <para>
    /// That is, this diagnostic suppressor will suppress the following diagnostic:
    /// <code>
    /// public partial class MyData : IData
    /// {
    ///     [DataProperty]
    ///     [field: JsonPropertyName("Name")]
    ///     public string Name => Get_Name();
    /// }
    /// </code>
    /// </para>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DataPropertyAttributeWithTargetsDiagnosticSuppressor : DiagnosticSuppressor
    {
        public const string DATA_PROPERTY_ATTRIBUTE = "global::ZBase.Foundation.Data.DataPropertyAttribute";

        /// <inheritdoc/>
        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(FieldAttributeListForDataProperty);

        /// <inheritdoc/>
        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
            {
                var syntaxNode = diagnostic.Location.SourceTree?.GetRoot(context.CancellationToken).FindNode(diagnostic.Location.SourceSpan);

                // Check that the target is effectively [field:] over a property declaration with at least one variable, which is the only case we are interested in
                if (syntaxNode is not AttributeTargetSpecifierSyntax attributeTarget
                    || attributeTarget.Parent.Parent is not PropertyDeclarationSyntax propertyDeclaration
                    || attributeTarget.Identifier.Kind() is not SyntaxKind.FieldDeclaration
                )
                {
                    continue;
                }

                var semanticModel = context.GetSemanticModel(syntaxNode.SyntaxTree);

                // Get the property symbol from the first variable declaration
                var declaredSymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration, context.CancellationToken);

                // Check if the property is using [DataProperty], in which case we should suppress the warning
                if (declaredSymbol is IPropertySymbol propertySymbol
                    && propertySymbol.HasAttribute(DATA_PROPERTY_ATTRIBUTE)
                )
                {
                    context.ReportSuppression(Suppression.Create(FieldAttributeListForDataProperty, diagnostic));
                }
            }
        }
    }
}
