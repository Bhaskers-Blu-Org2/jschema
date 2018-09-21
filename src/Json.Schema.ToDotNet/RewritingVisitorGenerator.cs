﻿// Copyright (c) Microsoft Corporation.  All Rights Reserved.
// Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Json.Schema.ToDotNet
{
    internal class RewritingVisitorGenerator
    {
        private const string NodeParameterName = "node";
        private const string KeyParameterName = "key";
        private const string VisitMethodName = "Visit";
        private const string VisitActualMethodName = "VisitActual";
        private const string VisitNullCheckedMethodName = "VisitNullChecked";
        private const string TypeParameterName = "T";
        private const string CountPropertyName = "Count";
        private const string AddMethodName = "Add";
        private const string ToArrayMethodName = "ToArray";

        private readonly TypeSyntax TypeParameterType = SyntaxFactory.ParseTypeName(TypeParameterName);
        private readonly TypeSyntax StringParameterType = SyntaxFactory.ParseTypeName("string");

        private readonly Dictionary<string, PropertyInfoDictionary> _classInfoDictionary;
        private readonly string _copyrightNotice;
        private readonly string _namespaceName;
        private readonly string _className;
        private readonly string _schemaName;
        private readonly string _kindEnumName;
        private readonly string _nodeInterfaceName;
        private readonly List<string> _generatedClassNames;

        private readonly LocalVariableNameGenerator _localVariableNameGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="RewritingVisitorGenerator"/> class.
        /// </summary>
        /// <param name="copyrightNotice">
        /// The copyright notice to display at the top of the file, or null if there is
        /// no copyright notice.
        /// </param>
        /// <param name="namespaceName">
        /// The name of the namespace into which the classes generated by this object
        /// are to be placed.
        /// </param>
        internal RewritingVisitorGenerator(
            Dictionary<string, PropertyInfoDictionary> classInfoDictionary,
            string copyrightNotice,
            string namespaceName,
            string className,
            string schemaName,
            string kindEnumName,
            string nodeInterfaceName,
            IEnumerable<string> generatedClassNames)
        {
            _classInfoDictionary = classInfoDictionary;
            _copyrightNotice = copyrightNotice;
            _namespaceName = namespaceName;
            _className = className;
            _schemaName = schemaName;
            _kindEnumName = kindEnumName;
            _nodeInterfaceName = nodeInterfaceName;
            _generatedClassNames = generatedClassNames.OrderBy(gn => gn).ToList();

            _localVariableNameGenerator = new LocalVariableNameGenerator();
        }

        internal string GenerateRewritingVisitor()
        {
            ClassDeclarationSyntax visitorClassDeclaration =
                SyntaxFactory.ClassDeclaration(_className)
                    .AddModifiers(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.AbstractKeyword))
                    .AddMembers(
                        GenerateVisitMethod(),
                        GenerateVisitActualMethod(),
                        GenerateVisitNullCheckedOneArgumentMethod(),
                        GenerateVisitNullCheckedTwoArgumentMethod())
                    .AddMembers(
                        GenerateVisitClassMethods());

            var usings = new List<string> { "System", "System.Collections.Generic", "System.Linq" };

            string summaryComment = string.Format(
                CultureInfo.CurrentCulture,
                Resources.RewritingVisitorSummary,
                _schemaName);

            return visitorClassDeclaration.Format(
                _copyrightNotice,
                usings,
                _namespaceName,
                summaryComment);
        }

        private MemberDeclarationSyntax GenerateVisitMethod()
        {
            return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(
                    SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                VisitMethodName)
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
                .AddParameterListParameters(
                    SyntaxFactory.Parameter(
                        SyntaxFactory.Identifier(NodeParameterName))
                        .WithType(
                            SyntaxFactory.ParseTypeName(_nodeInterfaceName)))
                .AddBodyStatements(
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.ThisExpression(),
                                SyntaxFactory.IdentifierName(VisitActualMethodName)))
                            .AddArgumentListArguments(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName(NodeParameterName)))))
                .WithLeadingTrivia(
                    SyntaxHelper.MakeDocComment(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.RewritingVisitorVisitMethodSummary,
                            _schemaName),
                        Resources.RewritingVisitorVisitMethodReturns,
                        new Dictionary<string, string>
                        {
                            [NodeParameterName] = Resources.RewritingVisitorVisitMethodNodeParameter
                        }));
        }

        private MemberDeclarationSyntax GenerateVisitActualMethod()
        {
            return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)),
                VisitActualMethodName)
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
                .AddParameterListParameters(
                    SyntaxFactory.Parameter(
                        SyntaxFactory.Identifier(NodeParameterName))
                        .WithType(
                            SyntaxFactory.ParseTypeName(_nodeInterfaceName)))
                .AddBodyStatements(
                    SyntaxFactory.IfStatement(
                        SyntaxHelper.IsNull(NodeParameterName),
                        SyntaxFactory.Block(
                            SyntaxFactory.ThrowStatement(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.ParseTypeName("ArgumentNullException"),
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    SyntaxFactory.Literal(NodeParameterName))))),
                                    default(InitializerExpressionSyntax))))),
                    SyntaxFactory.SwitchStatement(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(NodeParameterName),
                            SyntaxFactory.IdentifierName(_kindEnumName)))
                            .AddSections(
                                GenerateVisitActualSwitchSections()))
                .WithLeadingTrivia(
                    SyntaxHelper.MakeDocComment(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.RewritingVisitorVisitActualMethodSummary,
                            _schemaName),
                        Resources.RewritingVisitorVisitActualMethodReturns,
                        new Dictionary<string, string>
                        {
                            [NodeParameterName] = Resources.RewritingVisitorVisitActualMethodNodeParameter
                        }));
        }

        private SwitchSectionSyntax[] GenerateVisitActualSwitchSections()
        {
            // There is one switch section for each generated class, plus one for the default.
            var switchSections = new SwitchSectionSyntax[_generatedClassNames.Count + 1];

            int index = 0;
            foreach (string generatedClassName in _generatedClassNames)
            {
                string methodName = MakeVisitClassMethodName(generatedClassName);

                switchSections[index++] = SyntaxFactory.SwitchSection(
                    SyntaxFactory.SingletonList<SwitchLabelSyntax>(
                        SyntaxFactory.CaseSwitchLabel(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(_kindEnumName),
                                SyntaxFactory.IdentifierName(generatedClassName)))),
                    SyntaxFactory.SingletonList<StatementSyntax>(
                        SyntaxFactory.ReturnStatement(
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.IdentifierName(methodName),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.CastExpression(
                                                SyntaxFactory.ParseTypeName(generatedClassName),
                                                SyntaxFactory.IdentifierName(NodeParameterName)))))))));
            }

            switchSections[index] = SyntaxFactory.SwitchSection(
                SyntaxFactory.SingletonList<SwitchLabelSyntax>(
                    SyntaxFactory.DefaultSwitchLabel()),
                SyntaxFactory.SingletonList<StatementSyntax>(
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.IdentifierName(NodeParameterName))));

            return switchSections;
        }

        private MethodDeclarationSyntax GenerateVisitNullCheckedOneArgumentMethod()
        {
            return SyntaxFactory.MethodDeclaration(
                TypeParameterType,
                VisitNullCheckedMethodName)
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                .AddTypeParameterListParameters(
                    SyntaxFactory.TypeParameter(TypeParameterName))
                .AddConstraintClauses(
                    SyntaxFactory.TypeParameterConstraintClause(
                        SyntaxFactory.IdentifierName(TypeParameterName),
                        SyntaxFactory.SeparatedList(
                            new TypeParameterConstraintSyntax[]
                            {
                                SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint),
                                SyntaxFactory.TypeConstraint(
                                    SyntaxFactory.ParseTypeName(_nodeInterfaceName))
                            })))
                .AddParameterListParameters(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(NodeParameterName))
                        .WithType(TypeParameterType))
                .AddBodyStatements(
                    SyntaxFactory.IfStatement(
                        SyntaxHelper.IsNull(NodeParameterName),
                        SyntaxFactory.Block(
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)))),
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.CastExpression(
                            TypeParameterType,
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.IdentifierName(VisitMethodName),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.IdentifierName(NodeParameterName))))))));
        }

        private MethodDeclarationSyntax GenerateVisitNullCheckedTwoArgumentMethod()
        {
            return SyntaxFactory.MethodDeclaration(
                TypeParameterType,
                VisitNullCheckedMethodName)
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                .AddTypeParameterListParameters(
                    SyntaxFactory.TypeParameter(TypeParameterName))
                .AddConstraintClauses(
                    SyntaxFactory.TypeParameterConstraintClause(
                        SyntaxFactory.IdentifierName(TypeParameterName),
                        SyntaxFactory.SeparatedList(
                            new TypeParameterConstraintSyntax[]
                            {
                                SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint),
                                SyntaxFactory.TypeConstraint(
                                    SyntaxFactory.ParseTypeName(_nodeInterfaceName))
                            })))
                .AddParameterListParameters(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(NodeParameterName))
                        .WithType(TypeParameterType),
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(KeyParameterName))
                        .WithType(StringParameterType)
                        .WithModifiers(SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.RefKeyword))))
                .AddBodyStatements(
                    SyntaxFactory.IfStatement(
                        SyntaxHelper.IsNull(NodeParameterName),
                        SyntaxFactory.Block(
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)))),
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.CastExpression(
                            TypeParameterType,
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.IdentifierName(VisitMethodName),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.Argument(
                                            SyntaxFactory.IdentifierName(NodeParameterName))))))));
        }
        private MemberDeclarationSyntax[] GenerateVisitClassMethods()
        {
            // There is one VisitXxx method for each generated class.
            var visitClassMethods = new MemberDeclarationSyntax[_generatedClassNames.Count];

            int index = 0;
            foreach (string generatedClassName in _generatedClassNames)
            {
                visitClassMethods[index++] = GenerateVisitClassMethod(generatedClassName);
            }

            return visitClassMethods;
        }

        /// <summary>
        /// Generate the visitor method for one of the classes defined by the schema.
        /// </summary>
        /// <param name="className">
        /// The name of the class for which a visitor method is to be generated.
        /// </param>
        /// <returns>
        /// A method declaration for the vistor method.
        /// </returns>
        /// <example>
        /// <code>
        /// public virtual VisitLocation(Location node)
        /// {
        ///     if (node != null)
        ///     {
        ///         // GenerateVisitClassBodyStatements()
        ///     }
        ///
        ///     return node;
        /// }
        /// </code>
        /// </example>
        private MethodDeclarationSyntax GenerateVisitClassMethod(string className)
        {
            string methodName = MakeVisitClassMethodName(className);
            TypeSyntax generatedClassType = SyntaxFactory.ParseTypeName(className);

            return SyntaxFactory.MethodDeclaration(generatedClassType, methodName)
                .AddModifiers(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
                .AddParameterListParameters(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(NodeParameterName))
                    .WithType(generatedClassType))
                .AddBodyStatements(
                    SyntaxFactory.IfStatement(
                        SyntaxHelper.IsNotNull(NodeParameterName),
                        SyntaxFactory.Block(
                            GeneratePropertyVisits(className))),
                    SyntaxFactory.ReturnStatement(
                        SyntaxFactory.IdentifierName(NodeParameterName)));
        }

        /// <summary>
        /// Generate the code necessary to visit each property.
        /// </summary>
        /// <param name="className">
        /// The name of the class for which the visitor method is being generated.
        /// </param>
        /// <returns>
        /// The statements necessary to visit each property in the class.
        /// </returns>
        /// <remarks>
        /// It is only necessary to visit those properties which are themselves of a
        /// schema-defined type. Scalar properties can be visited directly. For properties
        /// of array type, each element must be visited.
        /// </remarks>
        /// <example>
        /// Visiting a class with one scalar-valued property and one array-valued property:
        /// <code>
        /// node.MessageDescriptor = VisitNullChecked(node.MessageDescriptor);
        /// 
        /// if (node.Locations != null)
        /// {
        ///     // GenerateArrayVisit()
        /// }
        /// </code>
        /// </example>
        private StatementSyntax[] GeneratePropertyVisits(string className)
        {
            var statements = new List<StatementSyntax>();

            PropertyInfoDictionary propertyInfoDictionary = _classInfoDictionary[className];
            foreach (KeyValuePair<string, PropertyInfo> entry in propertyInfoDictionary.OrderBy(kvp => kvp.Value.DeclarationOrder))
            {
                string propertyNameWithRank = entry.Key;
                PropertyInfo propertyInfo = entry.Value;

                // We only need to visit properties whose type is one of the classes
                // defined by the schema.
                if (!propertyInfo.IsOfSchemaDefinedType)
                {
                    continue;
                }

                int arrayRank = 0;
                bool isDictionary = false;
                string propertyName = propertyNameWithRank.BasePropertyName(out arrayRank, out isDictionary);

                TypeSyntax collectionType = propertyInfoDictionary.GetConcreteListType(propertyName);
                TypeSyntax elementType = propertyInfoDictionary[propertyNameWithRank].Type;

                ExpressionSyntax propertyAccessExpression =
                    SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(NodeParameterName),
                            SyntaxFactory.IdentifierName(propertyName));

                if (arrayRank == 0 && !isDictionary)
                {
                    // This is a simple scalar assignment.
                    statements.Add(GenerateScalarVisit(propertyAccessExpression));
                }
                else
                {
                    _localVariableNameGenerator.Reset();

                    StatementSyntax[] nullTestedStatements = null;
                    if (isDictionary)
                    {
                        nullTestedStatements = GenerateDictionaryVisit(arrayRank, propertyName);
                    }
                    else
                    {
                        nullTestedStatements = GenerateArrayVisit(
                            arrayRank,
                            nestingLevel: 0,
                            arrayValuedExpression: propertyAccessExpression);
                    }

                    statements.Add(
                        SyntaxFactory.IfStatement(
                            SyntaxHelper.IsNotNull(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName(NodeParameterName),
                                    SyntaxFactory.IdentifierName(propertyName))),
                            SyntaxFactory.Block(nullTestedStatements)));
                }
            }

            return statements.ToArray();
        }

        private StatementSyntax GenerateScalarVisit(
            ExpressionSyntax target,
            ExpressionSyntax source = null)
        {
            if (source == null)
            {
                source = target;
            }

            return
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        target,
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.IdentifierName(VisitNullCheckedMethodName),
                            SyntaxHelper.ArgumentList(source))));
        }

        private StatementSyntax[] GenerateDictionaryVisit(int arrayRank, string propertyName)
        {
            const string KeyVariableName = "key";
            const string KeysVariableName = "keys";
            const string KeysPropertyName = "Keys";
            const string ValueVariableName = "value";

            ExpressionSyntax dictionaryValue =
                SyntaxFactory.ElementAccessExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(NodeParameterName),
                        SyntaxFactory.IdentifierName(propertyName)),
                    SyntaxFactory.BracketedArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.IdentifierName(KeyVariableName)))));

            // The code to visit an individual dictionary element depends on whether the
            // elements are scalar values or arrays.
            StatementSyntax[] dictionaryElementVisitStatements;

            if (arrayRank == 0)
            {
                dictionaryElementVisitStatements = new StatementSyntax[]
                {
                    GenerateScalarVisit(dictionaryValue, SyntaxFactory.IdentifierName(ValueVariableName))
                };
            }
            else
            {
                ExpressionSyntax arrayValuedExpression =
                    SyntaxFactory.ElementAccessExpression(
                        SyntaxFactory.MemberAccessExpression(
                                   SyntaxKind.SimpleMemberAccessExpression,
                                   SyntaxFactory.IdentifierName(NodeParameterName),
                                   SyntaxFactory.IdentifierName(propertyName)),
                        SyntaxFactory.BracketedArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.IdentifierName(KeyVariableName)))));

                dictionaryElementVisitStatements = GenerateArrayVisit(
                    arrayRank,
                    nestingLevel: 0,
                    arrayValuedExpression: arrayValuedExpression);
            }

            return new StatementSyntax[]
            {
                // var keys = node.PropertyName.Keys.ToArray();
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxHelper.Var(),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(KeysVariableName),
                                default(BracketedArgumentListSyntax),
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.InvocationExpression(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                SyntaxFactory.MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    SyntaxFactory.IdentifierName(NodeParameterName),
                                                    SyntaxFactory.IdentifierName(propertyName)),
                                                SyntaxFactory.IdentifierName(KeysPropertyName)),
                                            SyntaxFactory.IdentifierName(ToArrayMethodName)),
                                        SyntaxHelper.ArgumentList())))))),

                // foreach (var key in keys)
                SyntaxFactory.ForEachStatement(
                    SyntaxHelper.Var(),
                    KeyVariableName,
                    SyntaxFactory.IdentifierName(KeysVariableName),
                    SyntaxFactory.Block(
                        SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(
                                SyntaxHelper.Var(),
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.VariableDeclarator(
                                        SyntaxFactory.Identifier(ValueVariableName),
                                        default(BracketedArgumentListSyntax),
                                        SyntaxFactory.EqualsValueClause(dictionaryValue))))),
                        SyntaxFactory.IfStatement(
                            SyntaxHelper.IsNotNull(ValueVariableName),
                            SyntaxFactory.Block(dictionaryElementVisitStatements))))
            };
        }

        private StatementSyntax[] GenerateArrayVisit(
            int arrayRank,
            int nestingLevel,
            ExpressionSyntax arrayValuedExpression)
        {
            ExpressionSyntax loopLimitExpression;
            if (nestingLevel == 0)
            {
                // node.Locations.Count
                loopLimitExpression = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    arrayValuedExpression,
                    SyntaxFactory.IdentifierName(CountPropertyName));
            }
            else
            {
                // value_0.Count
                loopLimitExpression = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(
                        LocalVariableNameGenerator.GetCollectionElementVariableName(nestingLevel - 1)),
                    SyntaxFactory.IdentifierName(CountPropertyName));
            }

            var statements = new List<StatementSyntax>();
            if (nestingLevel < arrayRank)
            {
                // We're not yet at the innermost level, so we need another for loop.
                string loopVariableName = LocalVariableNameGenerator.GetLoopIndexVariableName(nestingLevel);
                string outerLoopVariableName = LocalVariableNameGenerator.GetLoopIndexVariableName(nestingLevel - 1);
                string arrayElementVariableName = LocalVariableNameGenerator.GetCollectionElementVariableName(nestingLevel - 1);

                // For every level except the outermost, we need to get an array element and test whether
                // it's null.
                if (nestingLevel > 0)
                {

                    // var value_0 = node.Locations[index_0];
                    statements.Add(
                        SyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactory.VariableDeclaration(
                                SyntaxHelper.Var(),
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.VariableDeclarator(
                                        SyntaxFactory.Identifier(arrayElementVariableName),
                                        default(BracketedArgumentListSyntax),
                                        SyntaxFactory.EqualsValueClause(
                                            SyntaxFactory.ElementAccessExpression(
                                                arrayValuedExpression,
                                                SyntaxFactory.BracketedArgumentList(
                                                    SyntaxFactory.SingletonSeparatedList(
                                                        SyntaxFactory.Argument(
                                                            SyntaxFactory.IdentifierName(outerLoopVariableName)))))))))));
                }

                // for
                ForStatementSyntax forStatement = SyntaxFactory.ForStatement(
                        // (index_0 = 0;
                        SyntaxFactory.VariableDeclaration(
                            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.VariableDeclarator(loopVariableName)
                                    .WithInitializer(
                                        SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(
                                            SyntaxKind.NumericLiteralExpression,
                                            SyntaxFactory.Literal(0)))))),
                        default(SeparatedSyntaxList<ExpressionSyntax>),
                        // index_0 < value_0.Count;
                        SyntaxFactory.BinaryExpression(
                            SyntaxKind.LessThanExpression,
                            SyntaxFactory.IdentifierName(loopVariableName),
                            loopLimitExpression),
                        // ++index_0)
                        SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.PrefixUnaryExpression(
                                SyntaxKind.PreIncrementExpression,
                                SyntaxFactory.IdentifierName(loopVariableName))),
                        // { ... }
                        SyntaxFactory.Block(
                            GenerateArrayVisit(arrayRank, nestingLevel + 1, arrayValuedExpression)));

                if (nestingLevel > 0)
                {
                    statements.Add(
                        SyntaxFactory.IfStatement(
                            SyntaxHelper.IsNotNull(arrayElementVariableName),
                            SyntaxFactory.Block(
                                forStatement)));
                }
                else
                {
                    statements.Add(forStatement);
                }
            }
            else
            {
                string loopVariableName = LocalVariableNameGenerator.GetLoopIndexVariableName(nestingLevel - 1);

                // We're in the body of the innermost loop over array elements. This is
                // where we do the assignment. For arrays of rank 1, the assignment is
                // to an element of the property itself. For arrays of rank > 1, the
                // assignment is to an array element of a temporary variable representing
                // one of the elements of the property.
                ElementAccessExpressionSyntax elementAccessExpression;
                if (arrayRank == 1)
                {
                    // node.Location[index_0]
                    elementAccessExpression =
                        SyntaxFactory.ElementAccessExpression(
                            arrayValuedExpression,
                            SyntaxFactory.BracketedArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.IdentifierName(loopVariableName)))));
                }
                else
                {
                    string arrayElementVariableName = LocalVariableNameGenerator.GetCollectionElementVariableName(nestingLevel - 2);

                    // value_0[index_1]
                    elementAccessExpression =
                        SyntaxFactory.ElementAccessExpression(
                            SyntaxFactory.IdentifierName(arrayElementVariableName),
                            SyntaxFactory.BracketedArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(
                                        SyntaxFactory.IdentifierName(loopVariableName)))));
                }

                statements.Add(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            elementAccessExpression,
                            SyntaxFactory.InvocationExpression(
                                SyntaxFactory.IdentifierName(VisitNullCheckedMethodName),
                                SyntaxHelper.ArgumentList(elementAccessExpression)))));
            }

            return statements.ToArray();
        }

        private string MakeVisitClassMethodName(string className)
        {
            return VisitMethodName + className;
        }
    }
}
