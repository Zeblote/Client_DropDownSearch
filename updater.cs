//Drop Down Search
//By Zeblote (1163)



//Download Support_Updater
///////////////////////////////////////////////////////////////////////////

function ddsInstallUpdaterPrompt()
{
	%message = "<just:left>Drop Down Search might get new features or bug fixes in the future."
	@ " To make this much easier for you, an automatic updater is available! (Support_Updater by Greek2Me)"
	NL "\nJust click yes below to install it in the background. Click no to be reminded later.";

	messageBoxYesNo("Drop Down Search | Automatic Updates", %message, "ddsInstallUpdater();");
}

function ddsInstallUpdater()
{
	%url = "http://mods.greek2me.us/storage/Support_Updater.zip";
	%downloadPath = "Add-Ons/Support_Updater.zip";
	%className = "DDS_InstallUpdaterTCP";

	connectToURL(%url, "GET", %downloadPath, %className);
	messageBoxOK("Drop Down Search | Downloading Updater", "Trying to download the updater...");
}

function DDS_InstallUpdaterTCP::onDone(%this, %error)
{
	if(%error)
		messageBoxOK("Drop Down Search | Error :(", "Error downloading the updater:" NL %error NL "You'll be prompted again at a later time.");
	else
	{
		messageBoxOK("Drop Down Search | Updater Installed", "The updater has been installed.\n\nHave fun!");

		discoverFile("Add-ons/Support_Updater.zip");
		exec("Add-ons/Support_Updater/client.cs");
	}
}

schedule(1000, 0, "ddsInstallUpdaterPrompt");
$SupportUpdaterMigration = true;
