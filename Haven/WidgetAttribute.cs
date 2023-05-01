
namespace Haven;

// Attribute identifies a widget inside of a Layer and enables the Layer base class to add widgets automatically so long as the widgets are decorated with this attribute and AddWidgetsInternal() is called

[AttributeUsage(AttributeTargets.Field)]
public class WidgetAttribute : Attribute
{
	public WidgetAttribute() { }
}
