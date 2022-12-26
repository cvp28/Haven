
namespace Haven;

// Attribute identifies a widget inside of a Page and enables the Page base class to add widgets automatically so long as the widgets are decorated with this attribute and AddWidgetsInternal() is called

[AttributeUsage(AttributeTargets.Field)]
public class WidgetAttribute : Attribute
{
	public WidgetAttribute() { }
}
