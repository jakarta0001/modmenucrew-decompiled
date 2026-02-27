using ModMenuCrew.UI.Styles;
using UnityEngine;

namespace ModMenuCrew;

public static class GhostStyleMap
{
	public const byte NONE = 0;

	public const byte HEADER = 1;

	public const byte LABEL = 2;

	public const byte BUTTON = 3;

	public const byte TOGGLE = 4;

	public const byte BOX = 5;

	public const byte SECTION = 6;

	public const byte WINDOW = 7;

	public const byte SIDEBAR = 8;

	public const byte SIDEBAR_BTN = 9;

	public const byte SIDEBAR_BTN_ACTIVE = 10;

	public const byte STATUS_PILL = 11;

	public const byte SEPARATOR = 12;

	public const byte HIGHLIGHT = 13;

	public const byte CONTAINER = 14;

	public const byte SUBHEADER = 15;

	public static GUIStyle Get(byte id)
	{
		return (GUIStyle)(id switch
		{
			1 => GuiStyles.HeaderStyle, 
			2 => GuiStyles.LabelStyle, 
			3 => GuiStyles.ButtonStyle, 
			4 => GuiStyles.ToggleStyle, 
			5 => GUI.skin.box, 
			6 => GuiStyles.SectionStyle, 
			7 => GuiStyles.WindowStyle, 
			8 => GuiStyles.SidebarStyle, 
			9 => GuiStyles.SidebarButtonStyle, 
			10 => GuiStyles.SidebarButtonActiveStyle, 
			11 => GuiStyles.StatusPillStyle, 
			12 => GuiStyles.SeparatorStyle, 
			13 => GuiStyles.HighlightStyle, 
			14 => GuiStyles.ContainerStyle, 
			15 => GuiStyles.SubHeaderStyle, 
			_ => GUIStyle.none, 
		});
	}
}
