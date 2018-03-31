# NContext.Analyzers

Roslyn code analyzers for common NContext mistakes

## To demo

- Make sure the you have the Visual Studio Extensions workload installed, using the Visual Studio Installer.
- Open `Analyzer/NContext.Analyzers.sln`
- In the Solution Explorer, make sure `NContext.Analyzers.Vsix` is selected as the startup project.
- Press F5 to debug. This will launch a sandboxed Visual Studio instance with the analyzer installed as a VSIX package. 
- In the sandbox, open `DemoApp/NContext.Analyzers.Demo.sln`.
- Open `Program.cs`.
- The demo app has examples of safe and unsafe usages of `Let` extensions on `IServiceResponse<T>`.  The unsafe example will have the green squiggly underline that means "compiler warning". Mouseover for an explanation of why the code is bad. The same info is available in the Errors window.

## Notes on the implementation:
- The analyzer checks any method calls with `Let` in the name, so it will check `Let`, `LetAsync`, `AwaitLetAsync`, or anything else with `Let`.  This can be iterated on, so that we don't get false alarms if using other libraries with `Let` functions.
- Inside `Let` calls, the analyzer looks for method calls whose return type contains `IServiceResponse`, so it will catch methods returning `IServiceResponse<T>` or `Task<IServiceResponse<T>>` or anything similar.  This can be iterated on to also catch `DataResponse<T>`, `ErrorResponse<T>` or any type inheriting from `IServiceResponse<T>`.
- To avoid spamming the error log, the analyzer only reports the first offending method call inside the `Let`. If there are several and you just fix the first one, you will then be notified of the second one.
- The analyzer only looks for method calls, not method groups. It may be possible to subvert it when using method groups instead of lambda expressions as arguments to `Let`. From experience, I've always gotten compiler errors when trying this due to ambiguity between the overloads like `LetAsync(Action<T>)` and `LetAsync(Func<T, Task>)`. This can also be iterated on.

## Notes on the Visual Studio sandbox
- The sandbox instance will not have any of your normal VS settings or extensions installed.
- If you make any settings changes, they will be saved to the sandbox if you click the exit button of the sandbox instance.  If you just stop the debugger, the sandbox instance will close without saving settings.
- Note: The same sandbox is used for any VS extension projects you use, so if you've tried something like this before, you may have other experimental extensions installed. You can remove these by going to Tools > Extensions and Updates.

## Notes on the VSIX package
- I've implemented this as a Visual Studio extension to make demoing simpler.
- You can also make code analyzers into NuGet packages that can be installed on a per-project basis.
- If we decide to go forward with something like this, we could make it an additional package included with NContext, so that by installing NContext in a project, you would also get the analyzers, without requiring that project creators remember to install it, or that devs remember to install the VS extension.