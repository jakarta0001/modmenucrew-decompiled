using System.Collections.Generic;

namespace ModMenuCrew;

public class SectionDefinition
{
	public string Id;

	public string Name;

	public bool Visible = true;

	public string VisibleWhen;

	public List<ButtonDefinition> Buttons = new List<ButtonDefinition>();

	public List<SliderDefinition> Sliders = new List<SliderDefinition>();
}
