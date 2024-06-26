//Drop Down Search
//By Zeblote (1163)



//Download Support_Updater
///////////////////////////////////////////////////////////////////////////

if(!$Pref::DDS::DisableUpdater
	&& !$SupportUpdaterMigration
	&& !isFile("Add-Ons/Support_Updater.zip"))
{
	exec("./tcpclient.cs");
	exec("./updater.cs");
}



//Preferences
///////////////////////////////////////////////////////////////////////////

//Drop down lists added here will not be affected
$DDS::BlockCtrl["ServerSettingsGui_MaxPlayers"] = true;

//Whether we show results that contain the filter in the middle instead of the start
$DDS::ShowExtendedResults = true;

//Whether rows starting with the filter text are always first
$DDS::SeperateExtendedResults = true;

//Whether rows starting with the filter text are colored
$DDS::ColorBasicResults = false;

//Whether rows not starting with the filter text are colored
$DDS::ColorExtendedResults = true;

//Space to keep between the selected row and the scroll border
$DDS::SelectionPadding = 120;



//GuiPopUpMenuCtrl hooks
///////////////////////////////////////////////////////////////////////////

//Fix missing function errors
if(!isFunction(GuiPopUpMenuCtrl, onWake))
	eval("function GuiPopUpMenuCtrl::onWake(){}");

if(!isFunction(GuiPopUpMenuCtrl, onRemove))
	eval("function GuiPopUpMenuCtrl::onRemove(){}");

if(!isFunction(GuiTextListCtrl, onAdd))
	eval("function GuiTextListCtrl::onAdd(){}");

//Packaged functions
package DropDownSearch
{
	//New drop down list was created, initialize dds list
	function GuiPopUpMenuCtrl::onWake(%this)
	{
		parent::onWake(%this);

		%this.ddsInit();
	}

	//Drop down list was deleted, delete dds list
	function GuiPopUpMenuCtrl::onRemove(%this)
	{
		%this.ddsDestroy();

		parent::onRemove(%this);
	}

	//A line was added to the drop down list, refresh dds list
	function GuiPopUpMenuCtrl::add(%this, %name, %id, %scheme)
	{
		parent::add(%this, %name, %id, %scheme);

		%this.ddsUpdateRequired = true;
	}

	//A line was added to the drop down list, refresh dds list
	function GuiPopUpMenuCtrl::addFront(%this, %name, %id, %scheme)
	{
		parent::addFront(%this, %name, %id, %scheme);

		%this.ddsUpdateRequired = true;
	}

	//The drop down list was sorted, refresh dds list
	function GuiPopUpMenuCtrl::sort(%this)
	{
		parent::sort(%this);

		%this.ddsUpdateRequired = true;
	}

	//The drop down list was cleared, refresh dds list
	function GuiPopUpMenuCtrl::clear(%this)
	{
		parent::clear(%this);

		%this.ddsUpdateRequired = true;
	}

	//Force open the drop down list, open dds list instead
	function GuiPopUpMenuCtrl::forceOnAction(%this)
	{
		if(%this.ddsOverride)
			%this.ddsOpenMenu();
		else
			parent::forceOnAction(%this);
	}

	//Force close the drop down list, close dds list instead
	function GuiPopUpMenuCtrl::forceClose(%this)
	{
		if(%this.ddsOverride)
			%this.ddsCloseMenu();
		else
			parent::forceClose(%this);
	}

	//Internal text list was created (hack to get rows from drop down)
	function GuiTextListCtrl::onAdd(%this)
	{
		parent::onAdd(%this);

		$DDS::LastTextList = %this;
	}
};



//Creating a dds control
///////////////////////////////////////////////////////////////////////////

//Creates gui profiles required for the dds list
function GuiPopUpMenuCtrl::ddsCreateProfiles(%this)
{
	%profile = (strLen(%this.profile) ? %this.profile : GuiPopUpMenuProfile);
	%name = %profile.getName();
	%profile = %profile.getId();
	%profile.setName("DDS_TempProfileName");

	//Get mean between font and background color for highlighting
	%fontCol = getColorF(%profile.fontColor);
	%fillCol = getColorF(%profile.fillColor);

	%textCol = getColorI(vectoradd(%fontCol, vectorscale(vectorsub(%fillCol, %fontCol), 0.45)) SPC 1);
	%backCol = getColorI(vectoradd(%fontCol, vectorscale(vectorsub(%fillCol, %fontCol), 0.75)) SPC 1);

	//Text profile for tab completion hint
	if(!$DDS::TextProfile[%name])
	{
		%p = new GuiControlProfile("DDS_TextProfile_" @ %name : DDS_TempProfileName)
		{
			justify = left;
			fontColor = %textCol;
			textOffset = "0 0";
		};

		$DDS::TextProfile[%name] = %p;
	}

	//Text profile for search input
	if(!$DDS::InputProfile[%name])
	{
		%p = new GuiControlProfile("DDS_InputProfile_" @ %name : DDS_TempProfileName)
		{
			justify = left;
			canKeyFocus = true;
			textOffset = "0 0";
			border = false;
			opague = false;
			fillColor = "0 0 0 0";
		};

		$DDS::InputProfile[%name] = %p;
	}

	//Text profile for dds list
	if(!$DDS::ListProfile[%name])
	{
		%p = new GuiControlProfile("DDS_ListProfile_" @ %name : DDS_TempProfileName){
			fillColorHL = %backCol;
			fontColorHL = %fontCol;
			fontColors[2] = %fontCol;
			fontColors[3] = %textCol;
			mouseOverSelected = true;
		};

		$DDS::ListProfile[%name] = %p;
	}

	%profile.setName(%name);
}

//Creates better popup menu to replace original
function GuiPopUpMenuCtrl::ddsInit(%this)
{
	//Already has a dds list, do nothing
	if(isObject(%this.ddsButton))
		return;

	//In blocked list, do nothing
	if($DDS::BlockCtrl[%this.getName()])
		return;

	//Create dds profiles
	%this.ddsCreateProfiles();

	//Create dds button
	%this.ddsButton = new GuiMouseEventCtrl(DDS_ButtonCtrl)
	{
		position = "0 0";
		extent = %this.getExtent();
		ddsControl = %this;
		horizSizing = "width";
		vertSizing = "height";
	};

	//Add button to this control (can't use own .add method, it's broken?)
	GuiControl::add(%this, %this.ddsButton);

	//Have to copy the list before we can open this
	%this.ddsUpdateRequired = true;

	//Force opening this list should open the dds list instead
	%this.ddsOverride = true;
}

//Deletes dds controls
function GuiPopUpMenuCtrl::ddsDestroy(%this)
{
	//Doesn't have a dds list, do nothing
	if(!isObject(%this.ddsButton))
		return;

	%this.ddsCloseMenu();
	%this.ddsOverride = false;

	%this.ddsButton.delete();
}



//Opening a dds list
///////////////////////////////////////////////////////////////////////////

//Clicked popup, open menu
function DDS_ButtonCtrl::onMouseDown(%this)
{
	%this.ddsControl.ddsOpenMenu();
}

//Copy the rows from the drop down list to a buffer
function GuiPopUpMenuCtrl::ddsUpdateRows(%this)
{
	//If the list is empty, don't do anything
	if(%this.size() == 0)
	{
		%this.ddsRowCount = 0;
		return;
	}

	//%cnt = %this.size();
	//for(%i = 0; %i < %cnt; %i++)
	//{
	//	%this.ddsRow[%i] = %this.getRowText(%i);
	//	%this.ddsRowId[%i] = %this.getRowId(%i);
	//	%this.ddsRowNum[%this.ddsRowId[%i]] = %i;
	//}

	//Can't copy the rows from the drop down list directly,
	//so open the list and get them from the internal one...

	//Make sure we use the id, or this will fail
	%this = %this.getId();

	//Prepare control to ensure it does NOT call any callbacks
	%var = %this.variable;
	%cmd = %this.command;
	%alt = %this.altCommand;

	%this.variable   = "";
	%this.command    = "";
	%this.altCommand = "";

	%name = %this.getName();
	%this.setName("DDS_Temp");

	//Open the list
	%this.ddsOverride = false;
	%this.forceOnAction();

	//List opened, package should have set this var
	%list = $DDS::LastTextList;

	if(isObject(%list) && %list.rowCount() == %this.size())
	{
		%cnt = %list.rowCount();

		//Copy lines reversed, in case an id is used for multiple rows
		for(%i = %cnt - 1; %i >= 0; %i--)
		{
			%this.ddsRow[%i] = %list.getRowText(%i);
			%this.ddsRowId[%i] = %list.getRowId(%i);
			%this.ddsRowNum[%this.ddsRowId[%i]] = %i;
		}

		%this.ddsRowCount = %cnt;
	}
	else
	{
		error("Failed to copy lines from drop down list " @ %this @ ", the internal list was not created!");
		%this.ddsRowCount = 0;
	}

	//Close list and restore callbacks
	%this.forceClose();
	%this.ddsOverride = true;

	%this.setName(%name);
	%this.variable   = %var;
	%this.command    = %cmd;
	%this.altCommand = %alt;
}

//Get total width of all lines
function GuiPopUpMenuCtrl::ddsCalcListWidth(%this)
{
	//Create control to find width
	%profile = (strLen(%this.profile) ? %this.profile : GuiPopUpMenuProfile);

	//Enable auto resize
	%orig = %profile.autoSizeWidth;
	%profile.autoSizeWidth = true;

	%ctrl = new GuiTextCtrl(){profile = %profile;};

	//Find max width of all lines
	%width = 0;

	for(%i = 0; %i < %this.ddsRowCount; %i++)
	{
		%ctrl.setText(%this.ddsRow[%i]);

		//Save width of every line for later
		%w = getWord(%ctrl.getExtent(), 0) + 8;
		%this.ddsRowWidth[%i] = %w;

		if(%w > %width)
			%width = %w;
	}

	%ctrl.delete();
	%profile.autoSizeWidth = %orig;

	return %width;
}

//Open the dds list
function GuiPopUpMenuCtrl::ddsOpenMenu(%this)
{
	//Control is disabled - do nothing
	if(%this.enabled $= "0")
		return;

	//Menu already open - do nothing
	if(isObject(%this.ddsDialog))
		return;

	//Make sure we have the latest row list
	if(%this.ddsUpdateRequired)
		%this.ddsUpdateRows();


	//Some things that we will need often
	%thisPos = %this.getScreenPosition();
	%thisExt = %this.getExtent();
	%canvasExt = Canvas.getExtent();

	%profile = (strLen(%this.profile) ? %this.profile : GuiPopUpMenuProfile);


	//Create the background ctrl
	%this.ddsDialog = new GuiMouseEventCtrl(DDS_BackgroundCtrl)
	{
		position = "0 0";
		extent = %canvasExt;
		ddsControl = %this;
	};


	//Create the search field background
	%searchBorder = new GuiSwatchCtrl()
	{
		position = %thisPos;
		extent = vectorSub(%thisExt, "0 1");
		color = %profile.borderColor;
	};

	%searchFill = new GuiSwatchCtrl()
	{
		position = "1 1";
		extent = vectorSub(%thisExt, "2 1");
		color = %profile.fillColor;
	};

	%searchBorder.add(%searchFill);
	%this.ddsDialog.add(%searchBorder);


	//Create the search field
	%searchText = new GuiTextCtrl()
	{
		profile = $DDS::TextProfile[%profile.getName()];
		position = "5 1";
		extent = vectorSub(%thisExt, "5 1");
		text = "Search...";
	};

	%searchInput = new GuiTextEditCtrl(DDS_InputCtrl)
	{
		profile = $DDS::InputProfile[%profile.getName()];
		position = "2 1";
		extent = vectorSub(%thisExt, "2 1");
		tabComplete = true;
		command = %this @ ".ddsUpdateFilter($ThisControl.getValue());";
		altCommand = "$ThisControl.onTabComplete();";
		escapeCommand = %this @ ".ddsCloseMenu();";
		ddsControl = %this;
	};

	%searchBorder.add(%searchText);
	%searchBorder.add(%searchInput);
	%this.ddsSearchText = %searchText;


	//Calculate scroll rect
	%listWidth = %this.ddsCalcListWidth();

	%startY = getWord(%thisPos, 1) + getWord(%thisExt, 1) - 1;
	%listHeight = %this.ddsRowCount * (%profile.fontSize + 2);
	%canvasHeight = getWord(%canvasExt, 1);

	if(%startY + %listHeight + 12 > %canvasHeight)
	{
		//List doesn't fit below completely...
		%listHeight = %canvasHeight - %startY - 12;
		%listWidth += 16;

		%this.ddsScrollBar = true;
	}
	else
		%this.ddsScrollBar = false;

	//List not wide enough
	if(getWord(%thisExt, 0) > %listWidth)
		%listWidth = getWord(%thisExt, 0);

	//No entries
	if(!%this.ddsRowCount)
		%listHeight = 4;

	//Create scroll
	%scroll = new GuiScrollCtrl()
	{
		profile = %profile;
		position = getWord(%thisPos, 0) SPC %startY;
		extent = %listWidth SPC %listHeight + 2;

		hScrollBar = "alwaysOff";
		vScrollBar = "dynamic";
	};

	%this.ddsDialog.add(%scroll);
	%this.ddsScroll = %scroll;


	//Create text list
	%list = new GuiTextListCtrl(DDS_ListCtrl)
	{
		profile = $DDS::ListProfile[%profile.getName()];
		position = "1 1";
		extent = %listWidth SPC 0;
		command = %this @ ".ddsLineSelected();";
	};

	%scroll.add(%list);
	%this.ddsList = %list;


	//Add lines to list (no filter when opening list)
	for(%i = 0; %i < %this.ddsRowCount; %i++)
		%list.addRow(%i, %this.ddsRow[%i]);

	//Fix list not extending to the far right (?????)
	%list.resize(1, 1, 0, 0);


	//Push background dialog
	Canvas.pushDialog(%this.ddsDialog, 99);


	//Highlight the currently selected row
	%this.ddsSelectedRow = %this.ddsRowNum[%this.getSelected()];

	if(%this.ddsSelectedRow == -1)
		%this.ddsSelectedRow = 0;

	%list.selectLineNoCallback(%this.ddsSelectedRow);
	%this.ddsFilter = "";

	//Focus on search field
	%searchInput.makeFirstResponder(true);
	%searchInput.setCursorPos(0);


	//Create the actionmap for directional buttons
	//Seems like we can't detect pressing up/down on their own... sad
	%this.ddsActionMap = new ActionMap();
	%this.ddsActionMap.bindCmd("keyboard", "shift up", %this @ ".ddsStartMoveUp();", %this @ ".ddsStopMove();");
	%this.ddsActionMap.bindCmd("keyboard", "ctrl up", %this @ ".ddsStartMoveUp();", %this @ ".ddsStopMove();");
	%this.ddsActionMap.bindCmd("keyboard", "alt up", %this @ ".ddsStartMoveUp();", %this @ ".ddsStopMove();");
	%this.ddsActionMap.bindCmd("keyboard", "shift down", %this @ ".ddsStartMoveDown();", %this @ ".ddsStopMove();");
	%this.ddsActionMap.bindCmd("keyboard", "ctrl down", %this @ ".ddsStartMoveDown();", %this @ ".ddsStopMove();");
	%this.ddsActionMap.bindCmd("keyboard", "alt down", %this @ ".ddsStartMoveDown();", %this @ ".ddsStopMove();");
	%this.ddsActionMap.bind("mouse0", "zaxis", "ddsHandleScrolling");
	%this.ddsActionMap.push();

	$DDS::CurrentMenu = %this;
}



//Selecting and filtering lines
///////////////////////////////////////////////////////////////////////////

//Select a line in the list, without passing it to the original
function DDS_ListCtrl::selectLineNoCallback(%this, %id)
{
	%str = %this.command;
	%this.command = "";

	%this.setSelectedById(%id);
	%this.makeSelectedVisible();

	%this.command = %str;
}

//Move the control so the selected line is visible
function DDS_ListCtrl::makeSelectedVisible(%this)
{
	%scrollHeight = getWord(%this.getGroup().getExtent(), 1);
	%listHeight = getWord(%this.getExtent(), 1);

	%currPos = getWord(%this.getPosition(), 1);

	//Do nothing if the list doesn't have a scroll bar
	if(%listHeight < %scrollHeight)
		return;

	//Find center of the currently selected row
	%rowNum = %this.getRowNumById(%this.getSelectedId());
	%rowPosition = 1 + %rowNum * (%this.profile.fontSize + 2) + %this.profile.fontSize / 2;

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

//Update the scroll rect to fit new lines
function GuiPopUpMenuCtrl::ddsUpdateScrollRect(%this)
{
	%scrollPos = %this.ddsScroll.getPosition();
	%listPos = %this.ddsList.getPosition();
	%listExt = %this.ddsList.getExtent();

	%minWidth = getWord(%this.getExtent(), 0);

	%lineCount = %this.ddsList.rowCount();
	%listWidth = 0;

	//Find width of all currently visible lines
	for(%i = 0; %i < %lineCount; %i++)
	{
		%w = %this.ddsRowWidth[%this.ddsList.getRowId(%i)];

		if(%w > %listWidth)
			%listWidth = %w;
	}

	//Find height of lines
	%listHeight = %lineCount * (%this.ddsList.profile.fontSize + 2);

	%start = getWord(%scrollPos, 1);
	%canvasHeight = getWord(Canvas.getExtent(), 1);

	//List doesn't fit below completely...
	if(%start + %listHeight + 12 > %canvasHeight)
	{
		%listHeight = %canvasHeight - %start - 12;
		%listWidth += 16;

		%this.ddsScrollBar = true;
	}
	else
		%this.ddsScrollBar = false;

	//List not wide enough
	if(%minWidth > %listWidth)
		%listWidth = %minWidth;

	//No entries
	if(!%lineCount)
		%listHeight = 4;

	//Update scroll rect
	%this.ddsScroll.resize(getWord(%scrollPos, 0), getWord(%scrollPos, 1), %listWidth, %listHeight + 2);

	//Fix list not updating correctly (?????)
	%this.ddsScroll.remove(%this.ddsList);
	%this.ddsScroll.add(%this.ddsList);
}

//Update line filter
function GuiPopUpMenuCtrl::ddsUpdateFilter(%this, %filter)
{
	//Filter unchanged - do nothing
	if(%filter $= %this.ddsFilter)
		return;

	//Clear list
	%this.ddsList.clear();
	%this.ddsFilter = %filter;

	%filterLen = strLen(%filter);

	if(!%filterLen)
	{
		//Add all lines to list (no markup)
		for(%i = 0; %i < %this.ddsRowCount; %i++)
			%this.ddsList.addRow(%i, %this.ddsRow[%i]);

		//Select current line
		%this.ddsSelectedRow = %this.ddsRowNum[%this.getSelected()];
	}
	else
	{
		//Add all lines matching the filter
		for(%i = 0; %i < %this.ddsRowCount; %i++)
		{
			%curRow = %this.ddsRow[%i];
			%filterPos = strPos(strLwr(%curRow), strLwr(%filter));

			//Check whether the line should be added
			if(%filterPos == 0)
			{
				//Line starts with filter. Always add this one

				if($DDS::ColorBasicResults)
				{
					//Line should be colored (highlights filter)
					%curRow = "\c3" @ getSubStr(%curRow, 0, %filterPos)
							@ "\c2" @ getSubStr(%curRow, %filterPos, %filterLen)
							@ "\c3" @ getSubStr(%curRow, %filterPos + %filterLen, 255);
				}
			}
			else if(%filterPos > 0 && $DDS::ShowExtendedResults && !$DDS::SeperateExtendedResults)
			{
				//Line contains filter, extended results are enabled and not seperated

				if($DDS::ColorExtendedResults)
				{
					//Line should be colored (highlights filter)
					%curRow = "\c3" @ getSubStr(%curRow, 0, %filterPos)
							@ "\c2" @ getSubStr(%curRow, %filterPos, %filterLen)
							@ "\c3" @ getSubStr(%curRow, %filterPos + %filterLen, 255);
				}
			}
			else
				//Line doesn't contain filter or shouldn't be shown, ignore
				continue;

			//Add line to list
			%this.ddsList.addRow(%i, %curRow);
		}

		//If extended results are enabled and seperated, search again for them
		if($DDS::ShowExtendedResults && $DDS::SeperateExtendedResults)
		{
			for(%i = 0; %i < %this.ddsRowCount; %i++)
			{
				%curRow = %this.ddsRow[%i];
				%filterPos = strPos(strLwr(%curRow), strLwr(%filter));

				//Check whether the line should be added
				if(%filterPos > 0)
				{
					//Extended results don't start with filter

					if($DDS::ColorExtendedResults)
					{
						//Line should be colored (highlights filter)
						%curRow = "\c3" @ getSubStr(%curRow, 0, %filterPos)
								@ "\c2" @ getSubStr(%curRow, %filterPos, %filterLen)
								@ "\c3" @ getSubStr(%curRow, %filterPos + %filterLen, 255);
					}
				}
				else
					//Line is not an extended result, ignore
					continue;

				//Add line to list
				%this.ddsList.addRow(%i, %curRow);
			}
		}

		//If the previously selected line was removed, select the first one
		if(%this.ddsList.getRowNumById(%this.ddsSelectedRow) == -1)
			%this.ddsSelectedRow = %this.ddsList.getRowId(0);
	}

	%this.ddsUpdateTabHint();
	%this.ddsUpdateScrollRect();
	%this.ddsList.selectLineNoCallback(%this.ddsSelectedRow);
}

//Update the tab completion hint
function GuiPopUpMenuCtrl::ddsUpdateTabHint(%this)
{
	%len = strLen(%this.ddsFilter);

	if(%len)
	{
		%text = %this.ddsRow[%this.ddsSelectedRow];

		if(getSubStr(%text, 0, %len) $= %this.ddsFilter)
			%this.ddsSearchText.setText(%this.ddsFilter @ getSubStr(%text, %len, 255));
		else
			%this.ddsSearchText.setText("");
	}
	else
		%this.ddsSearchText.setText("Search...");
}

//Tab complete list
function DDS_InputCtrl::onTabComplete(%this)
{
	%this.ddsControl.ddsList.setSelectedById(%this.ddsControl.ddsSelectedRow);
}

//Selected a line - pass to original control
function GuiPopUpMenuCtrl::ddsLineSelected(%this)
{
	%row = %this.ddsList.getSelectedId();
	%this.ddsCloseMenu();

	//We can't directly select a row number, but we can open the list, select, close it
	%this.ddsOverride = false;
	
	%this.forceOnAction();
	$DDS::LastTextList.setSelectedRow(%row);
	%this.forceClose();

	%this.ddsOverride = true;
}



//Moving the selected line
///////////////////////////////////////////////////////////////////////////

//Start moving the selected line up
function GuiPopupMenuCtrl::ddsStartMoveUp(%this)
{
	%this.ddsMoveSelection(-1);

	cancel(%this.ddsMoveSchedule);
	%this.ddsMoveSchedule = %this.schedule(200, ddsRepeatMove, -1);
}

//Start moving the selected line down
function GuiPopupMenuCtrl::ddsStartMoveDown(%this)
{
	%this.ddsMoveSelection(1);

	cancel(%this.ddsMoveSchedule);
	%this.ddsMoveSchedule = %this.schedule(200, ddsRepeatMove, 1);
}

//Repeat moving the selected line
function GuiPopupMenuCtrl::ddsRepeatMove(%this, %dir)
{
	%this.ddsMoveSelection(%dir);

	cancel(%this.ddsMoveSchedule);
	%this.ddsMoveSchedule = %this.schedule(50, ddsRepeatMove, %dir);
}

//Stop moving the selected line
function GuiPopupMenuCtrl::ddsStopMove(%this)
{
	cancel(%this.ddsMoveSchedule);
}

//Handle moving the selected line
function GuiPopupMenuCtrl::ddsMoveSelection(%this, %dir)
{
	%curRow = %this.ddsList.getRowNumById(%this.ddsSelectedRow);
	%newRow = %curRow + %dir;
	
	if(%newRow < 0 || %newRow > %this.ddsList.rowCount() - 1)
		return;

	%this.ddsSelectedRow = %this.ddsList.getRowId(%newRow);
	%this.ddsList.selectLineNoCallback(%this.ddsSelectedRow);

	%this.ddsUpdateTabHint();
}

//Handle scrolling the selected line with mouse
function ddsHandleScrolling(%val, %a, %b)
{
	if(isObject($DDS::CurrentMenu))
	{
		$DDS::CurrentMenu.ddsStopMove();

		//If we have a scroll bar, don't scroll the list while mouse is inside it
		if($DDS::CurrentMenu.ddsScrollBar)
		{
			%scrollExt = $DDS::CurrentMenu.ddsScroll.getExtent();

			%scrollPos = $DDS::CurrentMenu.ddsScroll.getScreenPosition();
			%scrollX = getWord(%scrollPos, 0);
			%scrollY = getWord(%scrollPos, 1);

			%curPos = Canvas.getCursorPos();
			%curX = getWord(%curPos, 0);
			%curY = getWord(%curPos, 1);

			if(%curX > %scrollX
			&& %curX < %scrollX + getWord(%scrollExt, 0)
			&& %curY > %scrollY
			&& %curY < %scrollY + getWord(%scrollExt, 1))
				return;
		}

		if(%val > 1)
			$DDS::CurrentMenu.ddsMoveSelection(-1);
		else
			$DDS::CurrentMenu.ddsMoveSelection(1);
	}
}



//Closing a dds list
///////////////////////////////////////////////////////////////////////////

//Clicked popup background, close menu
function DDS_BackgroundCtrl::onMouseDown(%this)
{
	%this.ddsControl.ddsCloseMenu();
}

//Closes the popup menu
function GuiPopUpMenuCtrl::ddsCloseMenu(%this)
{
	//Menu not open - do nothing
	if(!isObject(%this.ddsDialog))
		return;

	Canvas.popDialog(%this.ddsDialog);
	%this.ddsDialog.delete();

	%this.ddsActionMap.pop();
	%this.ddsActionMap.delete();

	$DDS::CurrentMenu = 0;
}



//Initialization
///////////////////////////////////////////////////////////////////////////

//Recursively initialize all drop down lists created before loading this script
function SimGroup::ddsInitChildren(%this)
{
	if(%this.getClassName() $= "GuiPopUpMenuCtrl")
		%this.ddsInit();

	for(%i = %this.getCount() - 1; %i >= 0; %i--)
		%this.getObject(%i).ddsInitChildren();
}

//Activate package
activatePackage(DropDownSearch);

//Init all existing controls
//GuiGroup.ddsInitChildren();
