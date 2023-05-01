# Haven  
Haven is a C# .NET 7 library made for console apps that require a simple, customizable, and performant user interface.  
Haven supports Native AOT and has not (at least in my testing) had any bugs caused by trimming or whatnot.
  
Everything in Haven is fundamentally based around widgets. Widgets interact with the Haven engine in multiple ways.  
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
        LblHeading = new(2, 1, "Welcome to my app!");
        
        MnuMain = new(2, 3)
        {
            SelectedOptionStyle = MenuStyle.Highlighted
        };
        
        MnuMain.AddOption("Option1", delegate() { // your code here });
        MnuMain.AddOption("Option2", delegate() { // your code here });
        MnuMain.AddOption("Exit", App.Instance.SignalExit);
        
        base.AddWidgetsInternal();  // <- This has to be called if you plan on adding widgets using the [Widget] attribute
                                    //    Make sure you call it AFTER instantiating your widgets!
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


App a = App.Create();                   // Instantiates Haven with the default layer count

ExampleLayer LyrExample = new();        // Instantiates our custom layer

a.AddLayer("Example", LyrExample);      // When you add a layer, you give it a string ID to reference it with later on
a.SetLayer("Example", 0);               // Sets layer index 0 (bottom) to be the ExampleLayer. This calls OnShow() in the layer.

a.Run();
```
  
That will give you a basic page with a label and menu. You can use App.Instance.SetLayer() later on in your application to switch layers out at any time.
  
<hr>
  
## Rendering
Haven entirely uses VT escape sequences for rendering and supports 8-bit color through the VTColor static class (not an enum to make things easier).  
  
Basically, each widget is assigned its own VTRenderContext that it can draw to using its own drawing API. The VTRenderContext, essentially, is a simple StringBuilder that each drawing API method appends to whenever the user wants to set the foreground color, invert colors, draw text, etc.  
  
At render time, the Haven engine will instruct each widget to draw itself by calling that widget's Draw() method. Then it will grab the populated VTRenderContext buffer for that widget and append it to its own render buffer. When all is said and done, Haven's own render buffer will contain the virtual terminal instructions required to draw every widget to the screen. Haven, then converts that buffer to a sequence of UTF-8 bytes and writes it to the screen using WriteFile() on Windows and the libc write() syscall on Unix.  

All of this works just fine, but it should also be mentioned that Haven will only draw the frame if it contains changes relative to the last frame. If the current frame and the last frame are identical, Haven skips it to save on performance. This results in dramatically increased mainloop iterations per second (in most scenarios) and a much smoother experience on Linux, particularly.  
  
<hr>
  
## Extendability
Haven is built to be extended. Custom widgets are supported by extending the Widget base class.  
  
A custom widget must subscribe to the OnConsoleKey() callback that the engine will call if the widget is focused and the user has provided keyboard input.  
Additionally, each widget must implement a Draw() routine that Haven will call each iteration of the mainloop to draw the widget to the screen.  
  
```cs
public override void Draw()
{
    RenderContext.VTSetCursorPosition(2, 1);
    RenderContext.VTDrawText("Hello, World!");
}
```
