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

    // [<Literal>]
    // val FormatSelection: string = "fantomas/formatSelection"

    // [<Literal>]
    // val Configuration: string = "fantomas/configuration"

type LintFileRequest =
    {
        // OptionalLintParameters: OptionalLintParameters
        FilePath: string
        ParsedFileInfo: ParsedFileInformation option
    }

// type FormatDocumentRequest =
//     {
//         SourceCode: string

//         /// File path will be used to identify the .editorconfig options
//         /// Unless the configuration is passed
//         FilePath: string

//         /// Overrides the found .editorconfig.
//         Config: IReadOnlyDictionary<string, string> option

//         /// The current position of the cursor.
//         /// Zero-based
//         Cursor: FormatCursorPosition option
//     }

//     member IsSignatureFile: bool

// and FormatCursorPosition =
//     class
//         new: line: int * column: int -> FormatCursorPosition
//         val Line: int
//         val Column: int
//     end

// type FormatSelectionRequest =
//     {
//         SourceCode: string

//         /// File path will be used to identify the .editorconfig options
//         /// Unless the configuration is passed
//         FilePath: string

//         /// Overrides the found .editorconfig.
//         Config: IReadOnlyDictionary<string, string> option

//         /// Range follows the same semantics of the FSharp Compiler Range type.
//         Range: FormatSelectionRange
//     }

//     member IsSignatureFile: bool

// and FormatSelectionRange =
//     class
//         new: startLine: int * startColumn: int * endLine: int * endColumn: int -> FormatSelectionRange
//         val StartLine: int
//         val StartColumn: int
//         val EndLine: int
//         val EndColumn: int
//     end

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

    // abstract ClearCache: unit -> unit

    // abstract ConfigurationAsync: filePath: string * ?cancellationToken: CancellationToken -> Task<FantomasResponse>

    // abstract FormatDocumentAsync:
    //     FormatDocumentRequest * ?cancellationToken: CancellationToken -> System.Threading.Tasks.Task<FantomasResponse>

    // abstract FormatSelectionAsync:
    //     FormatSelectionRequest * ?cancellationToken: CancellationToken -> System.Threading.Tasks.Task<FantomasResponse>

    abstract VersionAsync:
        filePath: string * ?cancellationToken: CancellationToken -> Task<FSharpLintResponse>

    abstract LintFileAsync: LintFileRequest * ?cancellationToken: CancellationToken -> Task<FSharpLintResponse>
