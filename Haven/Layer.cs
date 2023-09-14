
using System.Reflection;

namespace HavenUI;

public class Layer
{
	internal int ZIndex;
	public Dictionary<ConsoleKey, Action<ConsoleKeyInfo>> KeyActions;
	
	// Internally used for keeping track of widgets
	internal List<Widget> Widgets;
	
	// Internally used for keeping track of delegates to UpdateTask functions
	internal List<Action<State>> UpdateTasks;
	
	// Tracks if Haven should respond to bound KeyActions for this layer
	public bool KeyActionsEnabled { get; set; } = true;
	
	public Layer()
	{
		Widgets = new();
		UpdateTasks = new();
		KeyActions = new();
	}
	
	internal void InitUpdateTasks()
	{
		var methods = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(m => m.IsDefined(typeof(UpdateTaskAttribute))).ToArray();

		foreach (var method in methods)
			UpdateTasks.Add(method.CreateDelegate(typeof(Action<State>), this) as Action<State>);
	}
	
	internal void InitWidgets()
	{
		var fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(p => p.IsDefined(typeof(WidgetAttribute))).ToArray();

		foreach (var field in fields)
			Widgets.Add(field.GetValue(this) as Widget);
	}
	
	internal void _OnShow(object[] Args)
	{
		foreach (var Task in UpdateTasks)
			Haven.AddUpdateTask($"{GetType().Name}.{Task.Method.Name}", Task);

		OnShow(Args);
	}
	
	internal void _OnHide()
	{
		foreach (var Task in UpdateTasks)
			Haven.RemoveUpdateTask($"{GetType().Name}.{Task.Method.Name}");

		OnHide();
	}
	
	/// <summary>
	/// Called whenever the layer needs to draw its UI
	/// </summary>
	public virtual void OnUIRender() { }

	/// <summary>
	/// Called whenever the layer is shown (SetLayer)
	/// </summary>
	public virtual void OnShow(object[] Args) { }
	
	/// <summary>
	/// Called whenever the layer is hidden
	/// </summary>
	public virtual void OnHide() { }
	
	/// <summary>
	/// Intended to be used for updating widget positions and dimensions in accordance with variable screen dimensions.
	/// </summary>
	public virtual void UpdateLayout(Dimensions d) { }
}