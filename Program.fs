module FsNote.Main

open System
open Markdig
open FSharp.CommandLine
open System.IO
open System.Text.RegularExpressions;
open Markdig.Syntax
open Markdig.Renderers
open Pchp.Core

let am2tex (code: string) =
  let am2t = new AMtoTeX()
  am2t.convert(PhpValue.Create(code)).ToString()

let inline processMath escape (text: string) =
  let regex = new Regex(@"@(?<text>(?:[^@\\]|\\.)*)@", RegexOptions.Compiled)
  let matches = regex.Matches(text) |> Seq.cast<Match> |> Seq.map (fun x -> x.Groups.["text"].Value)
  let mutable result = text
  for m in matches do
    let rep =
      let r = m |> String.replace "\\@" "@" 
                |> am2tex
                |> (if escape then String.replace "_" @"\_" else id)
      
      if m |> String.contains "\n" then
        sprintf "$$\n  %s\n$$" r
      else
        sprintf "$%s$" r

    result <- result |> String.replace (sprintf "@%s@" m) rep
  result

let inline templateOption () =
  commandOption {
    names ["t"; "template"]
    description "HTML template file. [title] to insert the title and [content] to insert the text."
    takes (format("%s").withNames ["file"])
    suggests (fun _ -> [CommandSuggestion.Files (Some "*.html")])
  }

type OutputType = OHtml | OMarkdown

let inline outputTypeOption () =
  commandOption {
    names ["o"; "output-type"]
    description "The output type."
    takes (format("h").asConst(OHtml))
    takes (format("html").asConst(OHtml))
    takes (format("m").asConst(OMarkdown))
    takes (format("markdown").asConst(OMarkdown))
  }

let inline outputDirectoryOption () =
  commandOption {
    names ["d"; "output-dir"]
    description "Directory to place the output files. [default: current directory]"
    takes (format("%s").withNames["dir"])
    suggests (fun _ -> [CommandSuggestion.Files None])
  }

let inline escapeUnderscoreOption () =
  commandFlag {
    names ["e"; "escape-underscore"]
    description "Escapes underscores in LaTeX code as '\\_'."
  }

let rec mainCommand () =
  command {
    name "fsnote"
    description "Generates HTML or Markdown from ASCIIMath-extended markdown."
    opt tf in templateOption() |> CommandOption.zeroOrExactlyOne
    opt ot in outputTypeOption() |> CommandOption.zeroOrExactlyOne |> CommandOption.whenMissingUse OHtml
    opt prefix in outputDirectoryOption() |> CommandOption.zeroOrExactlyOne |> CommandOption.whenMissingUse ""

    opt escapesUnderscore in escapeUnderscoreOption() |> CommandOption.zeroOrExactlyOne |> CommandOption.whenMissingUse false

    let templateHtml =
      tf |> Option.map (fun x -> File.ReadAllText(x))
         |> Option.defaultValue defaultHtmlTemplate

    do! Command.failOnUnknownOptions()
    let! args = Command.args
    do
      if args |> List.isEmpty then
        cprintfn ConsoleColor.Red "error: no files to process."
        mainCommand () |> Command.runAsEntryPoint [|"--help"|] |> ignore
    
    let pipeline = MarkdownPipelineBuilder().UseMathematics().UseAdvancedExtensions().Build();
    
    do args |> List.iter (fun file ->
      if File.Exists file then
        let text = File.ReadAllText file |> processMath escapesUnderscore

        let (output, ext) =
          match ot with
            | OMarkdown ->
              (text, "md")
            | OHtml ->
              let md = Markdown.Parse text
              let title =
                let il = md.Descendants() |> Seq.cast<HeadingBlock>
                                          |> Seq.tryHead
                                          |> Option.map (fun x -> x.Inline)
                let df = file |> Path.GetFileNameWithoutExtension
                use sw = new StringWriter() in
                  try
                    let hr = HtmlRenderer(sw)
                    hr.EnableHtmlForInline <- true
                    hr.Render(il.Value) |> ignore
                    sw.Flush()
                    sw.ToString()
                  with
                    | _ -> df
              let body = Markdown.ToHtml(text, pipeline)
              let result =
                templateHtml |> String.replace "[title]" title
                             |> String.replace "[content]" body
              (result, "html")
        
        let path =
          let x = sprintf "%s.%s" (Path.GetFileNameWithoutExtension file) ext
          if prefix <> "" && Directory.Exists prefix |> not then
            Directory.CreateDirectory(prefix) |> ignore
          Path.Combine(prefix, x)
        File.WriteAllText(path, output)
    )
    return 0
  }

[<EntryPoint>]
let main argv = 
  //"sum_(i=1)^n i^3=((n(n+1))/2)^2" |> am2tex |> printfn "%s"
  //Console.ReadLine() |> ignore
  //0
  mainCommand() |> Command.runAsEntryPoint argv
