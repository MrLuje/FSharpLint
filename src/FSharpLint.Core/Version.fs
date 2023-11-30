module FSharpLint.Core.Version

open System.Reflection
open FSharpLint.Framework.Configuration

let fsharpLintVersion =
    let assembly = typeof<Configuration>.Assembly
    assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
    |> Option.ofObj
    |> Option.map (fun a -> a.InformationalVersion)
    |> Option.defaultValue (Assembly.GetExecutingAssembly().GetName().Version.ToString())
