# Haven  
Haven is a C# .NET 7 library made for console apps that require a simple, customizable, and performant user interface.  
Haven supports Native AOT and has not (at least in my testing) had any bugs caused by trimming or whatnot.
  
Everything in Haven is fundamentally around widgets. Widgets interact with the Haven Engine in multiple ways.   
Widgets can be focused by modifying the App.FocusedWidget field.  
  
NOTE: You can refer to the singleton instance of Haven at any point by calling upon the static field "App.Instance".  
  
<hr>
  
## Layers
To start writing your first UI, you need to understand Haven's somewhat simple layering system.  
  
Basically, Haven will give you 3 layer indexes by default when you call App.Create() with no extra parameters. That is, you'll get layer indexes 0, 1, and 2 to use in your app. 0 displays below 1 which displays below 2. The higher the number, the higher the layer.    
  
To create a "main page" or "landing page" (whatever you want to call it) for your app, you must create a class that extends the Layer base class.  
When you do this, you'll be greeted with this blank slate (assuming you named your custom layer "ExampleLayer"):  
  
```cs
// ExampleLayer.cs

using Haven;

public class ExampleLayer : Layer
{

    public ExampleLayer() : base()
    {
        
    }

    public override void OnShow(App a)
    {
        
    }

    public override void OnHide(App a)
    {
        
    }

    // Called every frame after the update tasks and just before rendering to update
    // the position of widgets relative to the current screen dimensions
    public override void UpdateLayout(Dimensions d)
    {
        // Blank for now
    }
}
```
  
Now, you have to add the widgets that will display when this layer is shown. Say you wanted a layer with a menu that had 3 options on it and a label at the top of the screen. You might do something like this to achieve that:  
  
```cs
// ExampleLayer.cs

using Haven;

public class ExampleLayer : Layer
{
    [Widget] Menu MnuMain;          // Widgets can be added semi-automatically using the [Widget] attribute
    [Widget] Label LblHeading;      // This prevents you from having to do a bunch of Widgets.Add(~~~) calls for the internal
                                    // widget list in the Layer base class

    public ExampleLayer() : base()
    {
        MnuMain = new(2, 3)
        {
            SelectedOptionStyle = MenuStyle.Highlighted
        };
        
        MnuMain.AddOption("Option1", delegate() { // your code here });
        MnuMain.AddOption("Option2", delegate() { // your code here });
        MnuMain.AddOption("Exit", App.Instance.SignalExit);
        
        LblHeading = new(2, 1, "Welcome to my app!");
        
        base.AddWidgetsInternal();  // <- This has to be called if you plan on adding widgets using the [Widget] attribute
                                    // Make sure you call it AFTER instantiating your widgets!
    }

    public override void OnShow(App a)
    {
        a.FocusedWidget = MnuMain;
    }

    public override void OnHide(App a)
    {
        a.FocusedWidget = null;
    }

    // Called every frame after the update tasks and just before rendering to update
    // the position of widgets relative to the current screen dimensions
    public override void UpdateLayout(Dimensions d)
    {                                                 
        // Blank for now
    }
}
```
  
Once you have a custom layer, you need to instantiate it, add it to the haven instance, and then set it to display on a certain layer index.  
This will probably be done in your Program.cs file (but you can do it wherever):  
  
```cs
// Program.cs

using Haven;


App a = App.Create<ConWriteRenderer>(); // Instantiates Haven with the cross-platform renderer

ExampleLayer LyrExample = new();        // Instantiates our custom layer

a.AddLayer("Example", LyrExample);      // When you add a layer, you give it a string ID to reference it with later on
a.SetLayer("Example", 0);               // Sets layer index 0 (bottom) to be the ExampleLayer. This calls OnShow() in the layer.

a.Run();
```
  
That will give you a basic page with a label and menu. You can use App.Instance.SetLayer() later on in your application to switch layers out at any time.
  
<hr>
  
## Renderers 
Haven comes with two screen renderers by default and supports custom renderers via the type parameter of the App.Create<T>() function. 
The two stock renderers are as follows:  
  
### The ConWrite Renderer  
Built around a C# char buffer which allows for unicode support. Uses VT escape sequences to provide color support.  
  
### The WindowsNative Renderer
Uses P/Invoke to call a few Unicode-compatible Kernel32.dll Console API methods for incredibly low render times on Windows systems. Obviously does not work elsewhere.  
On a Windows system, WindowsNative should be used as it provides superior performance to the .NET BCL.  
On a Unix system, with their more performant console, the ConWrite renderer should yield FPS values in the hundreds to thousands for moderately complex apps.  
  
<hr>
  
## Extendability
Haven is built to be extended. Custom widgets and renderers are supported by extending the Widget or Renderer base class.
  
A custom widget must subscribe to the OnConsoleKey() callback that the engine will call if the widget is focused and the user has provided keyboard input.  
Additionally, each widget must implement a Draw() routine that the renderer will call each iteration of the render loop to draw the widget to the screen. 
  
The renderer, at render time, will pass its own instance to each widget's Draw() method. This is because the renderer provides implementations for the drawing API.  
For example, the following draw routine puts "Hello, World!" on the screen at an (x,y) of (2,1).  
  
```cs
public override void Draw(Renderer r)
{
    r.WriteStringAt(2, 1, "Hello, World!");
}
```
