# Command and Control for pwncat on Windows

This repository houses the stage one and stage two C2 libraries used by `pwncat` to
operate on a Windows target. When `pwncat` starts, "Stage Zero" is either `cmd.exe`
or `powershell.exe`. We assume we have all protections in place including Constrained-
Language Mode, AppLocker, and Defender. With this in mind, `pwncat` first uploads the
`loader.dll` file to a safe directory and executes it reflectively with `Install-Util`.
After `loader.dll` is running, it will reflectively load `stagetwo.dll` which is the 
primary C2 mechanism for `pwncat`.

## Protocol

This is not the best C2, but it works. It can be improved in multiple areas. Namely,
the communication standard is not consistent. Some methods take input as a Gzip/base64
blob while others take plaintext. At it's core, you can execute any public/static methods
within any type in `StageTwo.dll` by sending two lines of text. The first being the name
of the type, and the second being the name of the method. No arguments are passed to the
method. All methods then read their arguments directly from `stdin`. For example, to open
a file for reading, you would send the following payload:

```
File
open
C:\Windows\System32\drivers\etc\hosts
r
```

The output of individual methods are sent of `stdout`. In the case of `File.open`, a file
handle is written to `stdout` followed by a newline. If an error occurs or an excpetion is
caught, then a line beginning with `E:` is printed. Normally, the exception message follows.

## Features

In general this C2 supports the following features. These are subject to change frequently.
It's worth noting that during startup, a PowerShell `ConsoleHost` is started. `PSLogging` and
Constrained-Language Mode is disabled in this session regardless of system settings. This session
is persistent throughout the C2 execution, even across interactive sessions. It **does not** start
`powershell.exe`.

- Executing arbitrary processes (`Process.start`)
- Killing processes (`Process.kill`)
- Polling process state (`Process.poll`)
- Open/Close/Read/Write files (`File.open`, `File.close`, `File.read`, `File.write`)
- Compiling and executing arbitrary C# (`Reflection.compile`)
- Side-loading PowerShell Modules (`PowerShell.run`)
- Executing arbitrary PowerShell code and receiving JSON serialized PSObject and Exception results.
- Interactive Full Language Mode PowerShell session without `powershell.exe`

## Improvements

As I said before, this is not a great C2. I'm open to an improvements or suggestions. As I stated,
the actual API for making requests and passing arguments should probably be soldified and documented.
In the meantime, the `Windows` platform implementation in `pwncat` supports all the features outlined
above.