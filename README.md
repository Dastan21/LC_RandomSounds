# RandomSounds

Lethal Company mod that lets you add custom sounds to the game.
All of the sounds of a specific audio are randomly choosen and played synchronously between the players (if they have the mod too and the same custom audios).

## Install

- Install with a Mod Manager

OR

1. Make sure you have [BepInEx](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/) & [LC_API_V50](https://thunderstore.io/c/lethal-company/p/DrFeederino/LC_API_V50/) installed.
2. Move `RandomSounds.dll` and `RandomSounds` into `...\Lethal Company\BepInEx\plugins`.

## Add sounds

You must create a folder named after the audio you want to add sounds to.
Then add your audio files in the folder.

Example: `...\plugins\RandomSounds\ClownHorn1\Clown.mp3`

## Edit weights

You can customize the weight of each sound.
Create a file `weights.json` in on of the audio folder and set the weights you want.

For example, if you have 2 custom sounds `AirHorn1\Funny1.mp3` & `AirHorn1\Funny1.mp3`.

Here is an example of `weights.json`:
```json
[
	{
		"sound": "Funny1",
		"weight": 5,
	},
	{
		"sound": "Funny2",
		"weight": 2,
	},
	{
		"sound": "original", // reserved word for the original sound
		"weight": 0, // 0 or negative number to disable the sound
	}
]
```