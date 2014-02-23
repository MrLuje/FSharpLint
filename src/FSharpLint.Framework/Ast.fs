﻿(*
    FSharpLint, a linter for F#.
    Copyright (C) 2014 Matthew Mcveigh

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*)

namespace FSharpLint.Framework

module Ast =

    open System
    open System.Text.RegularExpressions
    open Microsoft.FSharp.Compiler.Range
    open Microsoft.FSharp.Compiler.Ast
    open Microsoft.FSharp.Compiler.SourceCodeServices
    open Tokeniser
    open AstVisitorBase

    /// Traverse an implementation file walking all the way down.
    let traverse parseTree (allVisitors: AstVisitorBase list) =
        let rec 
            traverseModuleOrNamespace visitors = function
            | SynModuleOrNamespace(identifier, isModule, moduleDeclarations, xmlDoc, attributes, access, range) ->
                let visit (visitor: AstVisitorBase) =
                    visitor.VisitModuleOrNamespace(identifier, isModule, moduleDeclarations, xmlDoc, attributes, access, range)

                let traverseModuleDeclaration =  visitors |> List.collect visit |> traverseModuleDeclaration

                moduleDeclarations |> List.iter traverseModuleDeclaration
        and 
            traverseModuleDeclaration visitors = function
            | SynModuleDecl.ModuleAbbrev(identifier, longIdentifier, _) -> ()
            | SynModuleDecl.NestedModule(componentInfo, moduleDeclarations, _, _) ->
                traverseComponentInfo visitors componentInfo

                moduleDeclarations |> List.iter (traverseModuleDeclaration visitors)
            | SynModuleDecl.Let(_, bindings, _) -> 
                let traverseBinding = traverseBinding visitors

                bindings |> List.iter traverseBinding
            | SynModuleDecl.DoExpr(_, expression, _) -> 
                traverseExpression visitors expression
            | SynModuleDecl.Types(typeDefinitions, _) -> 
                let traverseTypeDefinition = traverseTypeDefinition visitors

                typeDefinitions |> List.iter traverseTypeDefinition
            | SynModuleDecl.Exception(exceptionDefinition, _) -> 
                traverseExceptionDefinition visitors exceptionDefinition
            | SynModuleDecl.Open(longIdentifier, _) -> ()
            | SynModuleDecl.Attributes(attributes, _) -> ()
            | SynModuleDecl.HashDirective(hashDirective, _) -> ()
            | SynModuleDecl.NamespaceFragment(moduleOrNamespace) -> 
                traverseModuleOrNamespace visitors moduleOrNamespace
        and
            traverseBinding visitors = function
            | SynBinding.Binding(access, bindingKind, _, _, attributes, xmlDoc, _, pattern, _, expression, range, _) ->
                let visitors = visitors |> List.collect (fun visitor -> visitor.VisitBinding(pattern, range))
                traversePattern visitors pattern
                traverseExpression visitors expression
        and 
            traverseExceptionDefinition visitors = function
            | SynExceptionDefn.ExceptionDefn(exceptionRepresentation, members, _) -> 
                traverseExceptionRepresentation visitors exceptionRepresentation
                members |> List.iter (traverseMember visitors)
        and
            traverseExceptionRepresentation visitors = function
            | SynExceptionRepr.ExceptionDefnRepr(attributes, unionCase, identifier, xmlDoc, access, range) -> 
                let visit (visitor: AstVisitorBase) =
                    visitor.VisitExceptionRepresentation(attributes, unionCase, identifier, xmlDoc, access, range)

                let visitors = visitors |> List.collect visit

                traverseUnionCase visitors unionCase
        and
            traverseTypeDefinition visitors = function
            | SynTypeDefn.TypeDefn(componentInfo, typeRepresentation, members, _) -> 
                traverseComponentInfo visitors componentInfo
                traverseTypeRepresentation visitors typeRepresentation
                members |> List.iter (traverseMember visitors)
        and 
            traverseComponentInfo visitors = function
            | SynComponentInfo.ComponentInfo(attributes, typeParameters, typeConstraints, identifier, xmlDoc, _, access, range) ->
                let visit (visitor: AstVisitorBase) =
                    visitor.VisitComponentInfo(attributes, typeParameters, typeConstraints, identifier, xmlDoc, access, range)

                visitors |> List.collect visit |> ignore
        and 
            traverseTypeRepresentation visitors = function
            | ObjectModel(typeKind, members, _) -> 
                members |> List.iter (traverseMember visitors)
            | Simple(simpleTypeRepresentation, _) -> 
                traverseSimpleTypeRepresentation visitors simpleTypeRepresentation
        and
            traverseSimpleTypeRepresentation visitors = function
            | SynTypeDefnSimpleRepr.Union(access, unionCases, range) -> 
                unionCases |> List.iter (traverseUnionCase visitors)
            | SynTypeDefnSimpleRepr.Enum(enumCases, _) -> 
                enumCases |> List.iter (traverseEnumCase visitors)
            | SynTypeDefnSimpleRepr.Record(_, fields, _) -> 
                fields |> List.iter (traverseField visitors)
            | SynTypeDefnSimpleRepr.General(_, _, _, _, _, _, _, _) -> ()
            | SynTypeDefnSimpleRepr.TypeAbbrev(_, synType, _) -> 
                traverseType visitors synType
            | _ -> ()
        and 
            traverseUnionCase visitors = function
            | SynUnionCase.UnionCase(attributes, identifier, caseType, xmlDoc, access, range) -> 
                visitors |> List.iter (fun (visitor:AstVisitorBase) ->
                    visitor.VisitUnionCase(attributes, identifier, caseType, xmlDoc, access, range) |> ignore)
        and 
            traverseEnumCase visitors = function
            | SynEnumCase.EnumCase(attributes, identifier, constant, xmlDoc, range) -> 
                let visit (visitor:AstVisitorBase) =
                    visitor.VisitEnumCase(attributes, identifier, constant, xmlDoc, range) |> ignore

                visitors |> List.iter visit
        and
            traverseField visitors = function
            | SynField.Field(attributes, _, identifier, synType, _, xmlDoc, access, range) -> 
                let visit (visitor:AstVisitorBase) =
                    visitor.VisitField(attributes, identifier, synType, xmlDoc, access, range)

                let visitors = visitors |> List.collect visit

                traverseType visitors synType
        and
            traverseType visitors synType = 
                let traverseType = traverseType visitors

                match synType with
                | SynType.LongIdent(longIndetifier) -> ()
                | SynType.App(synType, _, types, _, _, _, _) -> 
                    traverseType synType
                    types |> List.iter traverseType
                | SynType.LongIdentApp(synType, identifier, _, types, _, _, _) -> 
                    traverseType synType
                    types |> List.iter traverseType
                | SynType.Tuple(types, _) ->    
                    types |> List.iter (fun (_, synType) -> traverseType synType)
                | SynType.Array(_, synType, _) -> 
                    traverseType synType
                | SynType.Fun(synType, synType1, _) -> 
                    traverseType synType
                    traverseType synType1
                | SynType.Var(_, _) -> ()
                | SynType.Anon(_) -> ()
                | SynType.WithGlobalConstraints(synType, typeConstraints, _) -> 
                    traverseType synType
                | SynType.HashConstraint(synType, _) -> 
                    traverseType synType
                | SynType.MeasureDivide(synType, synType1, _) -> 
                    traverseType synType
                    traverseType synType1
                | SynType.MeasurePower(synType, _, _) -> 
                    traverseType synType
                | SynType.StaticConstant(constant, _) -> ()
                | SynType.StaticConstantExpr(expression, _) -> 
                    traverseExpression visitors expression
                | SynType.StaticConstantNamed(synType, synType1, _) -> 
                    traverseType synType
                    traverseType synType1
        and
            traverseMatchClause visitors = function
            | SynMatchClause.Clause(pattern, expression, expression1, _, _) -> 
                traversePattern visitors pattern
                expression |> Option.iter (traverseExpression visitors)
                traverseExpression visitors expression1
        and
            traverseMember visitors synMember =
                let traverseType = traverseType visitors
                let traverseMember = traverseMember visitors
                let traverseBinding = traverseBinding visitors

                match synMember with
                | SynMemberDefn.Open(longIdentier, _) -> ()
                | SynMemberDefn.Member(binding, _) -> 
                    traverseBinding binding
                | SynMemberDefn.ImplicitCtor(_, attributes, patterns, identifier, _) -> 
                    patterns |> List.iter (traverseSimplePattern visitors)
                | SynMemberDefn.ImplicitInherit(synType, expression, identifier, _) -> 
                    traverseType synType
                    traverseExpression visitors expression
                | SynMemberDefn.LetBindings(bindings, _, _, _) -> 
                    bindings |> List.iter traverseBinding
                | SynMemberDefn.AbstractSlot(valueSignature, _, _) -> 
                    match valueSignature with
                    | SynValSig.ValSpfn(_, identifier, _, _, _, _, _, _, _, _, range) -> 
                        visitors |> List.iter (fun v -> v.VisitValueSignature(identifier, range) |> ignore)
                | SynMemberDefn.Interface(synType, members, _) -> 
                    traverseType synType
                    members |> Option.iter (fun members -> members |> List.iter traverseMember)
                | SynMemberDefn.Inherit(synType, identifier, _) -> 
                    traverseType synType
                | SynMemberDefn.ValField(field, _) -> 
                    traverseField visitors field
                | SynMemberDefn.NestedType(typeDefinition, _, _) -> 
                    traverseTypeDefinition visitors typeDefinition
                | SynMemberDefn.AutoProperty(attributes, _, identifier, synType, _, _, _, _, expression, _, _) -> 
                    synType |> Option.iter traverseType
                    traverseExpression visitors expression
        and
            traverseExpression visitors expression = 
                match expression with
                | SynExpr.Paren(expression, _, _, _) -> 
                    traverseExpression visitors expression
                | SynExpr.Quote(expression, _, expression1, _, _) -> 
                    traverseExpression visitors expression
                    traverseExpression visitors expression1
                | SynExpr.Const(constant, _) -> ()
                | SynExpr.Typed(expression, synType, _) -> 
                    traverseExpression visitors expression
                    traverseType visitors synType
                | SynExpr.Tuple(expressions, _, _) -> 
                    expressions |> List.iter (traverseExpression visitors)
                | SynExpr.ArrayOrList(_, expressions, _) -> 
                    expressions |> List.iter (traverseExpression visitors)
                | SynExpr.Record(synType, expression, _, _) -> 
                    expression |> Option.iter (fun (expression, _) -> traverseExpression visitors expression)
                | SynExpr.New(_, synType, expression, _) -> 
                    traverseExpression visitors expression
                    traverseType visitors synType
                | SynExpr.ObjExpr(synType, expressionAndIdentifier, bindings, interfaces, _, _) -> 
                    traverseType visitors synType
                    bindings |> List.iter (fun x -> traverseBinding visitors x)
                | SynExpr.While(_, expression, expression1, _) -> 
                    traverseExpression visitors expression
                    traverseExpression visitors expression1
                | SynExpr.For(_, identifier, expression, _, expression1, expression2, range) -> 
                    let visitors = visitors |> List.collect (fun visitor -> visitor.VisitFor(identifier, range))
                    traverseExpression visitors expression
                    traverseExpression visitors expression1
                    traverseExpression visitors expression2
                | SynExpr.ForEach(_, sequenceExpression, _, pattern, expression, expression1, _) -> 
                    traversePattern visitors pattern
                    traverseExpression visitors expression
                    traverseExpression visitors expression1
                | SynExpr.ArrayOrListOfSeqExpr(_, expression, _) -> 
                    traverseExpression visitors expression
                | SynExpr.CompExpr(_, _, expression, _) -> 
                    traverseExpression visitors expression
                | SynExpr.Lambda(_, _, simplePatterns, expression, _) -> 
                    traverseSimplePatterns visitors simplePatterns
                    traverseExpression visitors expression
                | SynExpr.MatchLambda(_, _, matchClauses, _, _) -> 
                    matchClauses |> List.iter (traverseMatchClause visitors)
                | SynExpr.Match(_, expression, matchClauses, _, _) -> 
                    traverseExpression visitors expression
                    matchClauses |> List.iter (traverseMatchClause visitors)
                | SynExpr.Do(expression, _) -> 
                    traverseExpression visitors expression
                | SynExpr.Assert(expression, _) -> 
                    traverseExpression visitors expression
                | SynExpr.App(_, _, expression, expression1, _) -> 
                    traverseExpression visitors expression
                    traverseExpression visitors expression1
                | SynExpr.TypeApp(expression, _, types, _, _, _, _) -> ()
                | SynExpr.LetOrUse(_, _, bindings, expression, _) -> 
                    bindings |> List.iter (traverseBinding visitors)
                    traverseExpression visitors expression
                | SynExpr.TryWith(expression, _, matchClauses, _, _, _, _) -> 
                    traverseExpression visitors expression
                    matchClauses |> List.iter (traverseMatchClause visitors)
                | SynExpr.TryFinally(expression, expression1, _, _, _) -> 
                    traverseExpression visitors expression
                    traverseExpression visitors expression1
                | SynExpr.Lazy(expression, _) -> 
                    traverseExpression visitors expression
                | SynExpr.Sequential(_, _, expression, expression1, _) -> 
                    traverseExpression visitors expression
                    traverseExpression visitors expression1
                | SynExpr.IfThenElse(expression, expression1, expression2, _, _, _, _) -> 
                    traverseExpression visitors expression
                    traverseExpression visitors expression1
                    expression2 |> Option.iter (traverseExpression visitors)
                | SynExpr.Ident(identifier) -> ()
                | SynExpr.LongIdent(_, identifier, _, _) -> ()
                | SynExpr.LongIdentSet(identifier, expression, _) -> 
                    traverseExpression visitors expression
                | SynExpr.DotGet(expression, _, identifier, _) -> 
                    traverseExpression visitors expression
                | SynExpr.DotSet(expression, identifier, expression1, _) -> 
                    traverseExpression visitors expression
                    traverseExpression visitors expression1
                | SynExpr.DotIndexedGet(expression, indexerArguments, _, _) -> 
                    traverseExpression visitors expression
                | SynExpr.DotIndexedSet(expression, indexerArguments, expression1, _, _, _) -> 
                    traverseExpression visitors expression
                    traverseExpression visitors expression1
                | SynExpr.NamedIndexedPropertySet(identifier, expression, expression1, _) -> 
                    traverseExpression visitors expression
                    traverseExpression visitors expression1
                | SynExpr.DotNamedIndexedPropertySet(expression, identifier, expression1, expression2, _) -> 
                    traverseExpression visitors expression
                    traverseExpression visitors expression1
                    traverseExpression visitors expression2
                | SynExpr.TypeTest(expression, synType, _) -> 
                    traverseExpression visitors expression
                    traverseType visitors synType
                | SynExpr.Upcast(expression, synType, _) -> 
                    traverseExpression visitors expression
                    traverseType visitors synType
                | SynExpr.Downcast(expression, synType, _) -> 
                    traverseExpression visitors expression
                    traverseType visitors synType
                | SynExpr.InferredUpcast(expression, _) -> 
                    traverseExpression visitors expression
                | SynExpr.InferredDowncast(expression, _) -> 
                    traverseExpression visitors expression
                | SynExpr.Null(_) -> ()
                | SynExpr.AddressOf(_, expression, _, _) -> 
                    traverseExpression visitors expression
                | SynExpr.TraitCall(typeParameters, memberSignature, expression, _) -> ()
                | SynExpr.JoinIn(expression, _, expression1, _) -> 
                    traverseExpression visitors expression
                    traverseExpression visitors expression1
                | SynExpr.ImplicitZero(_) -> ()
                | SynExpr.YieldOrReturn(_, expression, _) -> 
                    traverseExpression visitors expression
                | SynExpr.YieldOrReturnFrom(_, expression, _) -> 
                    traverseExpression visitors expression
                | SynExpr.LetOrUseBang(_, _, _, pattern, expression, expression1, _) -> 
                    traversePattern visitors pattern
                    traverseExpression visitors expression
                    traverseExpression visitors expression1
                | SynExpr.DoBang(expression, _) -> 
                    traverseExpression visitors expression
                | _ -> ()
        and
            traverseSimplePattern visitors = function
            | SynSimplePat.Id(identifier, _, isCompilerGenerated, _, _, range) -> 
                let visit (visitor: AstVisitorBase) = visitor.VisitIdPattern(identifier, isCompilerGenerated, range) |> ignore

                visitors |> List.iter visit
            | SynSimplePat.Typed(simplePattern, synType, _) -> 
                traverseSimplePattern visitors simplePattern
                traverseType visitors synType
            | SynSimplePat.Attrib(simplePattern, attributes, _) -> 
                traverseSimplePattern visitors simplePattern
        and
            traverseSimplePatterns visitors = function
            | SynSimplePats.SimplePats(simplePatterns, _) -> 
                simplePatterns |> List.iter (traverseSimplePattern visitors)
            | SynSimplePats.Typed(simplePatterns, synType, _) -> 
                traverseSimplePatterns visitors simplePatterns
                traverseType visitors synType
        and
            traversePattern visitors pattern = 
                match pattern with
                | SynPat.Const(constant, _) -> ()
                | SynPat.Wild(_) -> ()
                | SynPat.Named(pattern, identifier, x, access, range) -> 
                    let visit (visitor: AstVisitorBase) =
                        visitor.VisitNamedPattern(pattern, identifier, x, access, range)

                    let visitors = visitors |> List.collect visit

                    traversePattern visitors pattern
                | SynPat.Typed(pattern, synType, _) -> 
                    traversePattern visitors pattern
                    traverseType visitors synType
                | SynPat.Attrib(pattern, attributes, _) -> 
                    traversePattern visitors pattern
                | SynPat.Or(pattern, pattern1, _) -> 
                    traversePattern visitors pattern
                    traversePattern visitors pattern1
                | SynPat.Ands(patterns, _) -> 
                    patterns |> List.iter (traversePattern visitors)
                | SynPat.LongIdent(longIdentifier, identifier, _, constructorArguments, access, range) -> 
                    let visit (visitor: AstVisitorBase) =
                        visitor.VisitLongIdentPattern(longIdentifier, identifier, constructorArguments, access, range)

                    let visitors = visitors |> List.collect visit

                    traverseConstructorArguments visitors constructorArguments
                | SynPat.Tuple(patterns, _) -> 
                    patterns |> List.iter (traversePattern visitors)
                | SynPat.Paren(pattern, _) -> 
                    traversePattern visitors pattern
                | SynPat.ArrayOrList(_, patterns, _) -> 
                    patterns |> List.iter (traversePattern visitors)
                | SynPat.Record(patternsAndIdentifier, _) -> 
                    patternsAndIdentifier |> List.iter (fun (identifier, pattern) -> traversePattern visitors pattern)
                | SynPat.Null(_) -> ()
                | SynPat.OptionalVal(identifier, _) -> ()
                | SynPat.IsInst(synType, _) -> 
                    traverseType visitors synType
                | SynPat.QuoteExpr(expression, _) -> 
                    traverseExpression visitors expression
                | _ -> ()
        and
            traverseConstructorArguments visitors = function
            | SynConstructorArgs.Pats(patterns) -> 
                patterns |> List.iter (traversePattern visitors)
            | SynConstructorArgs.NamePatPairs(namePatterns, _) -> 
                namePatterns |> List.iter (fun (identifier, pattern) -> traversePattern visitors pattern)
        and
            traverseInterface visitors = function
            | SynInterfaceImpl.InterfaceImpl(synType, bindings, _) -> 
                traverseType visitors synType
                bindings |> List.iter (traverseBinding visitors)
        and 
            traverseParameter = function
            | SynTypar.Typar(identifier, _, _) -> ()

        let traverseModuleOrNamespace = traverseModuleOrNamespace allVisitors

        match parseTree with
            | ParsedInput.ImplFile(ParsedImplFileInput(_,_,_,_,_,moduleOrNamespaces,_))-> 
                moduleOrNamespaces |> List.iter traverseModuleOrNamespace
            | ParsedInput.SigFile _ -> ()

    /// Parse a file.
    let parse (checker:InteractiveChecker) projectOptions file input visitors =
        let parseFileResults = checker.ParseFileInProject(file, input, projectOptions)
        match parseFileResults.ParseTree with
        | Some tree -> 
            let checkFileResults = 
                checker.CheckFileInProject(parseFileResults, file, 0, input, projectOptions) 
                |> Async.RunSynchronously

            match checkFileResults with
            | CheckFileAnswer.Succeeded(res) -> 
                let visitors = visitors |> List.map (fun visitor -> visitor res)
                traverse tree visitors
            | res -> failwithf "Parsing did not finish... (%A)" res
        | None -> failwith "Something went wrong during parsing!"

    /// Parse a single string.
    let parseInput input visitors =
        let checker = InteractiveChecker.Create()

        let file = "/home/user/Dog.test.fsx"
        
        let projectOptions = checker.GetProjectOptionsFromScript(file, input)

        parse checker projectOptions file input visitors |> ignore