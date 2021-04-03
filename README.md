# FileDaddy
## Preface
This mod loader/manager is meant to simplify downloading and combining many different mods.
At the moment, FileDaddy has a very simple implementation only aimed to support the original Friday Night Funkin on Windows.
Yes that means popular exectuable mods such as VS Whitty aren't officially supported yet (although it might work anyhow).

## Before Starting
If your game is currently modded, please restore the game back to its original state. You can do this by manually deleting your modded files and replacing them back with the original ones. Or you can just download the game again to start off from scratch. This is important since FileDaddy uses your current assets folder of the game as its base without mods.

## Getting Started
### Unzipping
Please unzip FileDaddy.zip before use. The application will not work otherwise. If you don't know how, simply Right Click > Extract All... then click Extract.
Make sure its extracted to a location NOT synced with the cloud such as OneDrive, Google Drive, etc.
### Prerequisites
In order to get FileDaddy to open up and run, you'll need to install some prerequisites.

When you open FileDaddy.exe for the first time this window might appear.

![Error Message](https://media.discordapp.net/attachments/750914797838794813/827798772184121374/one_time_i_saw_my_dad.png)

This means you need to install some prerequisites. When clicking yes, it should take you to this page as shown below.

![Prerequisite Page](https://media.discordapp.net/attachments/750914797838794813/827646015640436771/unknown.png?width=1147&height=609)

Click the Download x64 that I highlighted in red as shown above. An installer will be downloaded shortly after. Open it then click Install. After it finished installing, you should now be able to open up FileDaddy.exe without any errors. However, if you still run into errors, your operating system might be 32-bit so download and install x86 as well.

Alternatively, you can just download the installer from [here](https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-desktop-5.0.4-windows-x64-installer) and run it before opening FileDaddy.exe and seeing the error message.

### Config
When finally opening FileDaddy.exe, first thing you want to do is click the Config button and set your Game Path to wherever your Funkin.exe is located. If you pick an executable that doesn't have an assets folder in the same directory, the console will spit out an error and tell you to try again.

### Installing Mods
You can get started with installing compatible mods by looking for this button ![button](https://media.discordapp.net/attachments/792245872259235850/827791904254066688/unknown.png) when exploring [Friday Night Funkin's GameBanana page](https://gamebanana.com/games/8694).

Note that this button will only work after you opened up the FileDaddy.exe for the first time.

Once you click it, a confirmation will appear on your browser asking if you want to open the application. Hit yes. If you don't want to continue seeing this confirmation just check off the Don't Ask Me From This Website before hitting yes.

A window will then open up confirming if you want to download the mod from FileDaddy.exe itself. Hit yes and a progress bar will let you know when the download is complete. Afterwards, the mod should appear on your mod list if it was successful.

### Building Your Loadout
After installing the mods you want, you can reorganize the priority of the list by simply dragging and dropping the rows in your desired position. You can also click the checkboxes to the left of the mods to enable/disable them. Mods higher up on the list will have higher priority. This means that if more than one mod modifies the same file, the highest mod's file will be the one used. 

Once you got your loadout setup, simply click the Build button and wait for it to notify you that it is finished building. After that, you can finally press the Launch button to start the game with your newly built loadout!

## Folder Structure
Folder structure of the mods are very important as FileDaddy relies on it to work properly. Mod folders must have an assets/ folder along with matching subfolders and files that match the game's folder structure. FileDaddy won't copy over anything if it cannot find the assets folder in the mod.  GameBanana's 1-click install button will also not show up if the directory doesn't have an assets folder.

If you want to make incompatible mods compatible just change their directories to have the proper folder structure. Also you can manually add mods to the list by creating a folder in the Mods directory located in the same folder as FileDaddy.exe.

Also try to have your zip have a folder with the name of the mod before assets rather than directly go into the assets folder. For example, amogus.zip/Amogus/assets is better than amogus.zip/assets. Names that appear on the mod list are grabbed from the folder names, so if it doesn't exist, I just use GameBanana API to fetch the GameBanana page's title as the folder name. This would mean that multiple variations from the same page that go directly to the assets folder will overwrite each other.

## Mod Loading Process
1. FileDaddy will first look through all the files in the assets folder with the .backup extension. It will restore all those backups by overwriting the modified files with them.
2. Next, FileDaddy will go in order from lowest to highest priority and copy the mods' assets files over to the matching assets location in the game's files. The original files will be renamed to have a .backup extension to be restored in step 1 when you build again. If the mod's assets path for a file doesn't exist in the game's files, it won't be copied over.

## Issues/Suggestions
If you have any issues with FileDaddy please fill out an issue on this GitHub page. Suggestions are welcomed as well. There's probably some things I haven't considered since I personally don't play/mod Friday Night Funkin frequently.

## Future Plans
- Support multiple exes
- Add metadata (currently only grabs name of mod from folder name)
- More compatibility with mods that don't have the assets folder
