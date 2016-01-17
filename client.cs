//Drop Down Search
//By Zeblote (1163)



//Download Support_Updater
///////////////////////////////////////////////////////////////////////////

exec("./updater.cs");



//Settings/Preferences
///////////////////////////////////////////////////////////////////////////

//Drop down lists added here will not be affected
$DDS::BlockCtrl["ServerSettingsGui_MaxPlayers"] = true;
$DDS::BlockCtrl["GuiEditorContentList"] = true;
$DDS::BlockCtrl["GuiEditorResList"] = true;

//Space to keep between the selected row and the scroll border
$DDS::SelectionPadding = 120;



//Initialization
///////////////////////////////////////////////////////////////////////////

//Creates better popup menu to replace original
function GuiPopUpMenuCtrl::ddsCreate(%this)
{
	//Already upgraded, do nothing
	if(isObject(%this.ddsControl))
		return;


	//Create DDS profiles
	if(!$DDS::STP[%this.profile.getName()])
	{
		%n1 = %this.profile.getName();
		%n2 = "DDS_SearchText_" @ %this.profile.getName();

		%cmd =        "%p = new GuiControlProfile(" @ %n2 @ ":" @ %n1 @ ") {";
		%cmd = %cmd @ "justify = left;";
		%cmd = %cmd @ "fontColor = \"255 0 0 255\";";
		%cmd = %cmd @ "textOffset = \"0 0\";";
		%cmd = %cmd @ "};";

		eval(%cmd);
		$DDS::STP[%this.profile.getName()] = %p;
	}

	if(!$DDS::SIP[%this.profile.getName()])
	{
		%n1 = %this.profile.getName();
		%n2 = "DDS_SearchInput_" @ %this.profile.getName();

		%cmd =        "%p = new GuiControlProfile(" @ %n2 @ ":" @ %n1 @ ") {";
		%cmd = %cmd @ "justify = left;";
		%cmd = %cmd @ "canKeyFocus = true;";
		%cmd = %cmd @ "textOffset = \"0 0\";";
		%cmd = %cmd @ "border = false;";
		%cmd = %cmd @ "opague = false;";
		%cmd = %cmd @ "fillColor = \"0 0 0 0\";";
		%cmd = %cmd @ "};";

		eval(%cmd);
		$DDS::SIP[%this.profile.getName()] = %p;
	}

	if(!$DDS::SLP[%this.profile.getName()])
	{
		%n1 = %this.profile.getName();
		%n2 = "DDS_SearchList_" @ %this.profile.getName();

		%color = %this.profile.fillColorHL;
		%r = getWord(%color, 0);
		%g = getWord(%color, 1);
		%b = getWord(%color, 2);
		%color = mFloor(%r * (4/5)) SPC mFloor(%g * (4/5)) SPC mFloor(%b * (4/5)) SPC 255;

		%cmd =        "%p = new GuiControlProfile(" @ %n2 @ ":" @ %n1 @ ") {";
		%cmd = %cmd @ "fillColorHL = \"" @ %color @ "\";";
		%cmd = %cmd @ "mouseOverSelected = true;";
		%cmd = %cmd @ "};";

		eval(%cmd);
		$DDS::SLP[%this.profile.getName()] = %p;
	}


	//Create DDS control
	%this.ddsControl = new GuiMouseEventCtrl(DDS_PopUpMenuCtrl)
	{
		position = "0 0";
		extent = %this.getExtent();

		lineCount = 0;
		realControl = %this;
	};

	//Add to itself using hack. This control won't render,
	//but it's only a mouse event ctrl so it works just fine!
	GuiControl::add(%this, %this.ddsControl);
}



//Line list
///////////////////////////////////////////////////////////////////////////

//We can't get a list of all lines in the original control,
//so we have to make our own list of them as we go...

//Adds a line to the end of the list
function DDS_PopUpMenuCtrl::addLineBack(%this, %line, %id)
{
	%c = %this.lineCount;

	%this.line[%c] = %line;
	%this.lineId[%c] = %id;
	%this.lineLookup[%id] = %c;
	%this.lineCount++;
}

//Adds a line to the start of the list
function DDS_PopUpMenuCtrl::addLineFront(%this, %line, %id)
{
	for(%i = %this.lineCount; %i > 0; %i--)
	{
		%this.line[%i] = %this.line[%i - 1];
		%this.lineId[%i] = %this.lineId[%i - 1];
		%this.lineLookup[%this.lineId[%i]] = %i;
	}
	
	%this.line[0] = %line;
	%this.lineId[0] = %id;
	%this.lineLookup[%id] = 0;
	%this.lineCount++;
}

//Clears line list
function DDS_PopUpMenuCtrl::clearLines(%this)
{
	%c = %this.lineCount;

	for(%i = 0; %i < %c; %i++)
	{
		%this.lineLookup[%this.lineId[%i]] = "";
		%this.lineWidth[%i] = "";
		%this.lineId[%i] = "";
		%this.line[%i] = "";
	}

	%this.lineCount = 0;
}



//Opening and closing the menu
///////////////////////////////////////////////////////////////////////////

//Opens better popup menu
function DDS_PopUpMenuCtrl::openMenu(%this)
{
	//Menu already open - do nothing
	if(%this.menuOpen)
		return;

	%profile = %this.realControl.profile;

	//Create the background ctrl
	%this.dialog = new GuiMouseEventCtrl(DDS_BackgroundCtrl)
	{
		position = "0 0";
		extent = Canvas.getExtent();
		ddsControl = %this;
	};


	//Create the search field background
	%searchBorder = new GuiSwatchCtrl()
	{
		position = %this.getScreenPosition();
		extent = vectorSub(%this.getExtent(), "0 1");
		color = %profile.borderColor;
	};

	%this.dialog.add(%searchBorder);

	%searchFill = new GuiSwatchCtrl()
	{
		position = "1 1";
		extent = vectorSub(%this.getExtent(), "2 1");
		color = %profile.fillColor;
	};

	%searchBorder.add(%searchFill);


	//Create the search field
	%searchTextProfile  = $DDS::STP[%profile];
	%searchInputProfile = $DDS::SIP[%profile];

	%searchText = new GuiTextCtrl()
	{
		profile = %searchTextProfile;
		position = "5 1";
		extent = vectorSub(%this.getExtent(), "5 1");
		text = "Search...";
	};

	%this.searchText = %searchText;
	%searchBorder.add(%searchText);

	%searchInput = new GuiTextEditCtrl(DDS_PopUpInputCtrl)
	{
		profile = %searchInputProfile;
		position = "2 1";
		extent = vectorSub(%this.getExtent(), "2 1");
		tabComplete = true;
		command = %this @ ".updateFilter($ThisControl.getValue());";
		altCommand = "$ThisControl.onTabComplete();";
		escapeCommand = %this @ ".closeMenu();";
		ddsControl = %this;
	};

	%this.searchInput = %searchText;
	%searchBorder.add(%searchInput);


	//Create scroll ctrl
	%rPos = %this.realControl.getScreenPosition();
	%rExt = %this.realControl.getExtent();

	%listWidth = %this.calcListWidth();

	%start = getWord(%rPos, 1) + getWord(%rExt, 1) - 1;
	%listHeight = %this.lineCount * (%profile.fontSize + 2);
	%canvasHeight = getWord(Canvas.getExtent(), 1);

	//List doesn't fit below completely...
	if(%start + %listHeight + 12 > %canvasHeight)
	{
		%listHeight = %canvasHeight - %start - 12;
		%listWidth += 16;
	}

	if(getWord(%rExt, 0) > %listWidth)
		%listWidth = getWord(%rExt, 0);

	%scroll = new GuiScrollCtrl()
	{
		profile = %profile;
		position = getWord(%rPos, 0) SPC %start;
		extent = %listWidth SPC %listHeight + 2;

		hScrollBar = "alwaysOff";
		vScrollBar = "dynamic";
	};

	%this.scroll = %scroll;
	%this.dialog.add(%scroll);


	//Create text list
	%list = new GuiTextListCtrl(DDS_PopUpListCtrl)
	{
		profile = $DDS::SLP[%profile.getName()];
		position = "1 1";
		extent = %listWidth SPC 0;
		command = %this @ ".onLineSelected();";
	};

	%this.list = %list;
	%scroll.add(%list);

	//Add lines to list (no filter when opening list)
	for(%i = 0; %i < %this.lineCount; %i++)
		%list.addRow(%this.lineId[%i], %this.line[%i]);

	//Sort lines alphabetically
	if(%this.sortLines)
		%list.sort(0, true);

	//Fix list not extending to the far right (?????)
	%list.resize(1, 1, 0, 0);


	//Highlight the currently selected row
	%this.selectedId = %this.realControl.getSelected();
	%list.selectLineNoCallback(%this.realControl.getSelected());

	%this.currFilter = "";


	//Push background dialog
	Canvas.pushDialog(%this.dialog, 99);

	//Focus on search field
	%searchInput.makeFirstResponder(true);
	%searchInput.setCursorPos(0);


	//Create the actionmap for directional buttons
	//Seems like we can't detect pressing up/down on their own... sad
	%this.actionMap = new ActionMap();
	%this.actionMap.bindCmd("keyboard", "shift up", %this @ ".startMoveUp();", %this @ ".stopMove();");
	%this.actionMap.bindCmd("keyboard", "ctrl up", %this @ ".startMoveUp();", %this @ ".stopMove();");
	%this.actionMap.bindCmd("keyboard", "alt up", %this @ ".startMoveUp();", %this @ ".stopMove();");
	%this.actionMap.bindCmd("keyboard", "shift down", %this @ ".startMoveDown();", %this @ ".stopMove();");
	%this.actionMap.bindCmd("keyboard", "ctrl down", %this @ ".startMoveDown();", %this @ ".stopMove();");
	%this.actionMap.bindCmd("keyboard", "alt down", %this @ ".startMoveDown();", %this @ ".stopMove();");
	%this.actionMap.push();

	%this.menuOpen = true;
}

//Closes the popup menu
function DDS_PopUpMenuCtrl::closeMenu(%this)
{
	//Menu not open - do nothing
	if(!%this.menuOpen)
		return;

	Canvas.popDialog(%this.dialog);
	%this.dialog.delete();

	%this.actionMap.pop();
	%this.actionMap.delete();

	%this.menuOpen = false;
}



//Selecting/filtering lines
///////////////////////////////////////////////////////////////////////////

//Select a line in the list, without passing it to the original
function DDS_PopUpListCtrl::selectLineNoCallback(%this, %id)
{
	%str = %this.command;
	%this.command = "";

	%this.setSelectedById(%id);
	%this.makeSelectedVisible();

	%this.command = %str;
}

//Move the control so the selected line is visible
function DDS_PopUpListCtrl::makeSelectedVisible(%this)
{
	%scrollHeight = getWord(%this.getGroup().getExtent(), 1);
	%listHeight = getWord(%this.getExtent(), 1);

	%currPos = getWord(%this.getPosition(), 1);

	//Do nothing if the list doesn't have a scroll bar
	if(%listHeight < %scrollHeight)
		return;

	//Find center of the currently selected row
	%rowNum = %this.getRowNumById(%this.getSelectedId());
	%rowPosition = %rowNum * (%this.profile.fontSize + 2) + %this.profile.fontSize / 2;

	%distanceUp = %rowPosition + %currPos;
	%distanceDown = (%scrollHeight - %currPos) - %rowPosition;

	//Adjust list to have 120px above and below the line
	if(%distanceUp < $DDS::SelectionPadding)
		%currPos += $DDS::SelectionPadding - %distanceUp;

	if(%distanceDown < $DDS::SelectionPadding)
		%currPos -= $DDS::SelectionPadding - %distanceDown;

	//Move list
	%this.resize(1, %currPos, getWord(%this.getExtent(), 0), %listHeight);
}

//Update line filter
function DDS_PopUpMenuCtrl::updateFilter(%this, %filter)
{
	//Filter unchanged - do nothing
	if(%filter $= %this.currFilter)
		return;

	//Clear list
	%this.list.clear();

	%this.currFilter = %filter;

	if(strLen(%filter) == 0)
	{
		//Add all lines to list
		for(%i = 0; %i < %this.lineCount; %i++)
			%this.list.addRow(%this.lineId[%i], %this.line[%i]);

		if(%this.sortLines)
			%this.list.sort(0, true);

		//Select current line
		%this.selectedId = %this.realControl.getSelected();
	}
	else
	{
		//Add all lines matching the filter
		for(%i = 0; %i < %this.lineCount; %i++)
		{
			if(getSubStr(%this.line[%i], 0, strLen(%filter)) $= %filter)
				%this.list.addRow(%this.lineId[%i], %this.line[%i]);
		}

		if(%this.sortLines)
			%this.list.sort(0, true);

		//Does the previously selected line still exist?
		if(%this.list.getRowNumById(%this.selectedId) == -1)
			%this.selectedId = %this.list.getRowId(0);
	}

	%this.updateTabHint();
	%this.updateScrollRect();
	%this.list.selectLineNoCallback(%this.selectedId);
}

//Update the tab completion hint
function DDS_PopUpMenuCtrl::updateTabHint(%this)
{
	%f = %this.currFilter;

	if(strLen(%f))
		%this.searchText.setText(%f @ getSubStr(%this.list.getRowTextById(%this.selectedId), strLen(%f), 255));
	else
		%this.searchText.setText("Search...");
}



//List math
///////////////////////////////////////////////////////////////////////////

//Get total width of all lines
function DDS_PopUpMenuCtrl::calcListWidth(%this)
{
	//Create control to find width
	%profile = %this.realControl.profile;

	%orig = %profile.autoSizeWidth;
	%profile.autoSizeWidth = true;

	%ctrl = new GuiTextCtrl(){profile = %profile;};

	//Find max width of all lines
	%width = 0;

	for(%i = 0; %i < %this.lineCount; %i++)
	{
		%ctrl.setText(%this.line[%i]);

		%w = getWord(%ctrl.getExtent(), 0) + 8;
		%this.lineWidth[%i] = %w;

		if(%w > %width)
			%width = %w;
	}

	%ctrl.delete();
	%profile.autoSizeWidth = %orig;

	return %width;
}

//Update the scroll rect to fit new lines
function DDS_PopUpMenuCtrl::updateScrollRect(%this)
{
	%scrollPos = %this.scroll.getPosition();
	%listPos = %this.list.getPosition();
	%listExt = %this.list.getExtent();

	%minWidth = getWord(%this.getExtent(), 0);

	%lineCount = %this.list.rowCount();
	%listWidth = 0;

	//Find width of all currently visible lines
	for(%i = 0; %i < %lineCount; %i++)
	{
		%w = %this.lineWidth[%this.lineLookup[%this.list.getRowId(%i)]];

		if(%w > %listWidth)
			%listWidth = %w;
	}

	//Find height of lines
	%listHeight = %lineCount * (%this.list.profile.fontSize + 2);

	%start = getWord(%scrollPos, 1);
	%canvasHeight = getWord(Canvas.getExtent(), 1);

	//List doesn't fit below completely...
	if(%start + %listHeight + 12 > %canvasHeight)
	{
		%listHeight = %canvasHeight - %start - 12;
		%listWidth += 16;
	}

	//List not wide enough
	if(%minWidth > %listWidth)
		%listWidth = %minWidth;

	if(!%lineCount)
		%listHeight = 4;

	//Update scroll rect
	%this.scroll.resize(getWord(%scrollPos, 0), getWord(%scrollPos, 1), %listWidth, %listHeight + 2);

	//Fix list not updating correctly (?????)
	%this.scroll.remove(%this.list);
	%this.scroll.add(%this.list);
}



//Gui callbacks
///////////////////////////////////////////////////////////////////////////

//Clicked popup, open menu
function DDS_PopUpMenuCtrl::onMouseDown(%this)
{
	%this.openMenu();
}

//Clicked popup background, close menu
function DDS_BackgroundCtrl::onMouseDown(%this)
{
	%this.ddsControl.closeMenu();
}

//Selected a line - pass to original control
function DDS_PopUpMenuCtrl::onLineSelected(%this)
{
	%id = %this.list.getSelectedId();

	%this.closeMenu();
	%this.realControl.setSelected(%id);
}

//Tab complete list
function DDS_PopUpInputCtrl::onTabComplete(%this)
{
	%this.ddsControl.list.setSelectedById(%this.ddsControl.selectedId);
}



//Input callbacks
///////////////////////////////////////////////////////////////////////////

//Start moving the selected line up
function DDS_PopUpMenuCtrl::startMoveUp(%this)
{
	%this.moveSelection(-1);

	cancel(%this.moveSchedule);
	%this.moveSchedule = %this.schedule(200, repeatMove, -1);
}

//Start moving the selected line down
function DDS_PopUpMenuCtrl::startMoveDown(%this)
{
	%this.moveSelection(1);

	cancel(%this.moveSchedule);
	%this.moveSchedule = %this.schedule(200, repeatMove, 1);
}

//Repeat moving the selected line
function DDS_PopUpMenuCtrl::repeatMove(%this, %dir)
{
	%this.moveSelection(%dir);

	cancel(%this.moveSchedule);
	%this.moveSchedule = %this.schedule(50, repeatMove, %dir);
}

//Stop moving the selected line
function DDS_PopUpMenuCtrl::stopMove(%this)
{
	cancel(%this.moveSchedule);
}

//Handle moving the selected line
function DDS_PopUpMenuCtrl::moveSelection(%this, %dir)
{
	%curRow = %this.list.getRowNumById(%this.selectedId);
	%newRow = %curRow + %dir;

	if(%newRow < 0 || %newRow > %this.list.rowCount() - 1)
		return;

	%this.selectedId = %this.list.getRowId(%newRow);
	%this.list.selectLineNoCallback(%this.selectedId);

	%this.updateTabHint();
}



//GuiPopUpMenuCtrl hooks
///////////////////////////////////////////////////////////////////////////

//Fix missing function error
if(!isFunction(GuiPopUpMenuCtrl, onRemove))
	eval("function GuiPopUpMenuCtrl::onRemove(){}");

//Packaged functions
package DropDownSearch
{
	//Stuff added to popup menu
	function GuiPopUpMenuCtrl::add(%this, %name, %id, %scheme)
	{
		parent::add(%this, %name, %id, %scheme);

		//It's not possible to get the contents of a drop down control by script
		//However if this is the first line added to it, we can still save it
		if(!isObject(%this.ddsControl))
		{
			if(%this.size() != 1 || $DDS::BlockCtrl[%this.getName()])
				return;

			%this.ddsCreate();
		}
		
		%this.ddsControl.addLineBack(%name, %id);
	}

	//Stuff added to popup menu (front)
	function GuiPopUpMenuCtrl::addFront(%this, %name, %id, %scheme)
	{
		parent::addFront(%this, %name, %id, %scheme);

		//It's not possible to get the contents of a drop down control by script
		//However if this is the first line added to it, we can still save it
		if(!isObject(%this.ddsControl))
		{
			if(%this.size() != 1 || $DDS::BlockCtrl[%this.getName()])
				return;

			%this.ddsCreate();
		}
		
		%this.ddsControl.addLineFront(%name, %id);
	}

	//Popup menu sorted
	function GuiPopUpMenuCtrl::sort(%this)
	{
		parent::sort(%this);

		if(isObject(%this.ddsControl))
			%this.ddsControl.sortLines = true;
	}

	//Popup menu cleared
	function GuiPopUpMenuCtrl::clear(%this)
	{
		parent::clear(%this);

		if(isObject(%this.ddsControl))
			%this.ddsControl.clearLines();
	}

	//Force open popup menu
	function GuiPopUpMenuCtrl::forceOnAction(%this)
	{
		if(isObject(%this.ddsControl))
			%this.ddsControl.openMenu();
		else
			parent::forceOnAction(%this);
	}

	//Force close popup menu
	function GuiPopUpMenuCtrl::forceClose(%this)
	{
		if(isObject(%this.ddsControl))
			%this.ddsControl.closeMenu();
		else
			parent::forceClose(%this);
	}

	//Deleted popup menu
	function GuiPopUpMenuCtrl::onRemove(%this)
	{
		if(isObject(%this.ddsControl))
		{
			%this.ddsControl.closeMenu();
			%this.ddsControl.delete();
		}

		parent::onRemove(%this);
	}
};

//Activate package
activatePackage(DropDownSearch);
