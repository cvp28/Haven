
namespace Haven;

public class WidgetGroup
{
	public List<Widget> Widgets;
	public Action<WidgetGroup> OnShow;
	public Action<WidgetGroup> OnHide;

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

		if (OnShow is not null)
			OnShow(this);
	}

	public void Hide()
	{
		foreach (Widget w in Widgets)
			w.Visible = false;

		if (OnHide is not null)
			OnHide(this);
	}

	public void ToggleVisibility()
	{
		foreach (Widget w in Widgets)
			w.Visible = !w.Visible;
	}
}