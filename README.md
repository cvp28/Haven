# Quantum  
Haven is a C# .NET 6 library made for console apps that require a simple, customizable, and performant user interface.  
  
Everything in Haven is built around widgets. Widgets interact with the Haven Engine in multiple ways.  
  
Each widget can subscribe to the OnConsoleKey() callback that the engine will call if the widget is focused and the user has provided keyboard input.  
Additionally, each widget must implement a Draw() routine that the renderer will call each iteration of the render loop to draw the widget to the screen.  
  
The engine is built such that custom widgets can be constructed from these guidelines by extending the Widget base class.  
Widgets can be focused by modifying the Engine.FocusedWidget field.  
  
## Renderers 
Haven comes with two screen renderers by default and supports custom renderers via the type parameter of the Engine.Initialize() function. 
The two stock renderers are as follows:  
  
### The Pure-C# Cross-Platform Renderer  
Implemented using the System.Console API and relies on ANSI escape sequences to produce color.  
How to use: Engine.Initialize<Screen>();  
  
### The Kernel32-Based Renderer  
Implemented using P/Invoke calls to GetStdHandle() and WriteConsoleOutputW() in Kernel32.dll on Windows systems.  
How to use: Engine.Initialize<NativeScreen>();  
  
On a Windows system, Kernel32 should be used as it provides superior performance to the .NET API.  
On a Unix system, with their more performance-oriented console, the Pure-C# renderer should yield FPS values well beyond 1,000 for moderately complex apps.  
