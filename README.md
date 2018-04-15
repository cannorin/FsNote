FsNote
------

```
usage: fsnote [options]

Generates HTML or Markdown from ASCIIMath-extended markdown.

options:
-t, --template=<file>             HTML template file. [title] to insert the title and [content] to insert the text.
-o, --output-type={h[tml]|m[arkdown]}
                                  The output type.
-d, --output-dir=<dir>            Directory to place the output files. [default: current directory]
-e, --escape-underscore[+|-]      Escapes underscores in LaTeX code as '\_'.

```

## Example

```
## Foo

This is an inline ASCIIMath expression: @ sum_(i=1)^n i^3=((n(n+1))/2)^2 @

Below is a block ASCIIMath expression:

@
  sum_(i=1)^n i^3=((n(n+1))/2)^2
@

This is an escaped 'circ' symbol: @ \@ @

```

with an appropriate LaTeX renderer (e.g. HTML + MathJax), the above will be rendered like:

![it's cool, isn't it?](https://i.imgur.com/yTBO78i.png)

## Build

`nuget restore` then `msbuild`.

## License

Apache 2
