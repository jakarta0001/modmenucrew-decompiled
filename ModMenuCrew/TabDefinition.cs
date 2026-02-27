using System.Collections.Generic;

namespace ModMenuCrew;

public class TabDefinition
{
	public string Id;

	public string Name;

	public string Icon;

	public string Context;

	public bool Enabled;

	public List<SectionDefinition> Sections = new List<SectionDefinition>();

	public List<TeleportLocation> Locations = new List<TeleportLocation>();
}
