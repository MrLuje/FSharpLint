module FSharpLint.Client.Contracts

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open FSharpLint.Application
open FSharpLint.Core
open FSharp.Compiler.Text
open FSharpLint.Framework.Suggestion

module Methods =

    [<Literal>]
    val Version: string = "fsharplint/version"

    [<Literal>]
    val LintFile: string = "fsharplint/lintfile"

type LintFileRequest =
    {
        // OptionalLintParameters: OptionalLintParameters
        FilePath: string
        ParsedFileInfo: ParsedFileInformation option
    }

type SelectionRange =
    class
        new: startLine: int * startColumn: int * endLine: int * endColumn: int -> SelectionRange
        val StartLine: int
        val StartColumn: int
        val EndLine: int
        val EndColumn: int
    end

type SuggestedFixC = {
    /// Text to be replaced.
    FromText:string

    /// Location of the text to be replaced.
    FromRange:SelectionRange

    /// Text to replace the `FromText`, i.e. the fix.
    ToText:string
}

[<NoEquality; NoComparison>]
type WarningDetailsC = {
    /// Location of the code that prompted the suggestion.
    Range:SelectionRange

    /// Suggestion message to describe the possible problem to the user.
    Message:string

    /// Information to provide an automated fix.
    SuggestedFix:SuggestedFixC option
}

/// A lint "warning", sources the location of the warning with a suggestion on how it may be fixed.
[<NoEquality; NoComparison>]
type LintWarningC = {
    /// Unique identifier for the rule that caused the warning.
    RuleIdentifier:string

    /// Unique name for the rule that caused the warning.
    RuleName:string

    /// Path to the file where the error occurs.
    FilePath:string

    /// Text that caused the error (the `Range` of the content of `FileName`).
    ErrorText:string

    /// Details for the warning.
    Details:WarningDetailsC
}

type FSharpLintResponse =
    {
        Code: int
        FilePath: string
        Content: string option
        Result: LintWarningC list
    }

type FSharpLintService =
    inherit System.IDisposable

    abstract VersionAsync:
        filePath: string * ?cancellationToken: CancellationToken -> Task<FSharpLintResponse>

    abstract LintFileAsync: LintFileRequest * ?cancellationToken: CancellationToken -> Task<FSharpLintResponse>
