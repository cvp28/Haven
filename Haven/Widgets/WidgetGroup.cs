
namespace Haven;

public class WidgetGroup
{
	private List<Widget> Widgets;

	public WidgetGroup(params Widget[] Widgets)
	{
		this.Widgets = new();

		foreach (Widget w in Widgets)
		{
			if (this.Widgets.Contains(w))
				continue;
			else
				this.Widgets.Add(w);
		}
	}

	public void Show()
	{
		foreach (Widget w in Widgets)
			w.Visible = true;
	}

	public void Hide()
	{
		foreach (Widget w in Widgets)
			w.Visible = false;
	}

	public void ToggleVisibility()
	{
		foreach (Widget w in Widgets)
			w.Visible = !w.Visible;
	}
}