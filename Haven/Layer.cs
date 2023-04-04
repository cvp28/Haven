
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace Haven;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class Layer
{
	internal int ZIndex;
	internal List<Widget> Widgets;

	// Internally used for keeping track of delegates to UpdateTask functions
	protected List<Action<State>> _UpdateTasks;

	public Layer()
	{
		Widgets = new();
		_UpdateTasks = new();

		var methods = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(m => m.IsDefined(typeof(UpdateTaskAttribute))).ToArray();

		foreach (var method in methods)
			_UpdateTasks.Add(method.CreateDelegate(typeof(Action<State>), this) as Action<State>);
	}

	/// <summary>
	/// Call this function AFTER initializing the layer widgets
	/// </summary>
	protected void AddWidgetsInternal()
	{
		var fields = GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(p => p.IsDefined(typeof(WidgetAttribute))).ToArray();

		foreach (var field in fields)
			Widgets.Add(field.GetValue(this) as Widget);
	}

	/// <summary>
	/// Intended to be used for updating widget positions and dimensions in accordance with variable screen dimensions.
	/// </summary>
	public abstract void UpdateLayout(Dimensions d);

	internal void _OnShow(App a, object[] Args)
	{
		foreach (var Task in _UpdateTasks)
			a.AddUpdateTask($"{GetType().Name}.{Task.Method.Name}", Task);

		OnShow(a, Args);
	}

	internal void _OnHide(App a)
	{
		foreach (var Task in _UpdateTasks)
			a.RemoveUpdateTask($"{GetType().Name}.{Task.Method.Name}");

		OnHide(a);
	}

	public abstract void OnShow(App a, object[] Args);
	public abstract void OnHide(App a);
}
