namespace ModMenuCrew;

public static class GhostOpcode
{
	public const byte WIN_BEGIN = 1;

	public const byte WIN_END = 2;

	public const byte WIN_UPDATE = 3;

	public const byte HORIZ_BEGIN = 16;

	public const byte HORIZ_END = 17;

	public const byte VERT_BEGIN = 18;

	public const byte VERT_END = 19;

	public const byte SCROLL_BEGIN = 20;

	public const byte SCROLL_END = 21;

	public const byte AREA_BEGIN = 22;

	public const byte AREA_END = 23;

	public const byte SECTION_BEGIN = 24;

	public const byte SECTION_END = 25;

	public const byte LABEL = 32;

	public const byte BUTTON = 33;

	public const byte BOX = 34;

	public const byte SPACE = 35;

	public const byte FLEX_SPACE = 36;

	public const byte SEPARATOR = 37;

	public const byte TOGGLE = 38;

	public const byte SLIDER = 39;

	public const byte TEXTURE = 40;

	public const byte SIDEBAR_BEGIN = 48;

	public const byte SIDEBAR_END = 49;

	public const byte TAB_BTN = 50;

	public const byte TAB_CONTENT_BEGIN = 51;

	public const byte TAB_CONTENT_END = 52;

	public const byte LICENSE_BADGE = 53;

	public const byte BRAND_HEADER = 54;

	public const byte NAV_HEADER = 55;

	public const byte FOOTER_STATUS = 56;

	public const byte PLAYER_LIST = 57;

	public const byte PLAYER_LOOP_BEGIN = 58;

	public const byte GRID_BEGIN = 160;

	public const byte GRID_END = 161;

	public const byte PLAYER_CARD = 162;

	public const byte ACTION_SIDEBAR_BEGIN = 163;

	public const byte ACTION_SIDEBAR_END = 164;

	public const byte ACTION_BTN = 165;

	public const byte PLAYER_CARD_MINI = 166;

	public const byte GRID_ROW_BEGIN = 167;

	public const byte GRID_ROW_END = 168;

	public const byte ACTION_BTN_PERMITTED = 169;

	public const byte COLOR_SET = 64;

	public const byte COLOR_RESTORE = 65;

	public const byte HEADER_DRAW = 80;

	public const byte RESIZE_HANDLE = 81;

	public const byte GLOW_BORDER = 82;

	public const byte INVOKE_ACTION = 96;

	public const byte INVOKE_IF_SELECTED = 97;

	public const byte IF_PREMIUM = 112;

	public const byte IF_SELECTED = 113;

	public const byte IF_INGAME = 114;

	public const byte IF_EXPANDED = 115;

	public const byte INTEGRITY_CHECK = 240;

	public const byte WATERMARK = 241;

	public const byte TRAP = 254;

	public const byte END_PROGRAM = byte.MaxValue;
}
