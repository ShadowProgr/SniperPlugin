# Sniper Plugin

This is a 3rd party plugin made for GoManager by SLxTnT. It supports sniping Pokémons posted on Pokedex100 discord server.

## Installation

Installation is pretty simple and is the same as any GoManager plugin.

* Download latest version from [releases](https://github.com/ShadowProgr/SniperPlugin/releases)
* Put the **SniperPlugin.dll** in the **Plugins** folder of GoManager

## Usage

* Select account you want to snipe on inside GoManager
* Select **Plugins** -> **ShadowProgr's Sniper Plugin**
* Confirm your choice
* Select the configuration file to use

### Configuration file

All configuration files must be in the following format
```
Pokemon;Amount;Minimum IV;Minimum CP
```
For example, this following config will catch

* 2 Blisseys
* 3 Dragonites with at least 80IV
* 5 Tyranitars with at least 80IV and 2500CP

```
Blissey;2;0;0
Dragonite;3;80;0
Tyranitar;5;80;2500
```

You may also use this config to catch 50 Pokémons that have 100IV
```
Any;50;100;0
```

Adding the "request" parameter to config line will also request that Pokémon in #candies_vip channel
```
Togetic;5;0;1500;request
```

## Changelog

```
v1.1
* Now able to request Pokémon in #candies_vip
* Stop is now properly working
* Remade parsing system

v1.0
* Initial release
```

## Credits

* *GoManager* by *SLxTnT* - the tool this plugin was developed for
* [*Discord.Net*](https://github.com/RogueException/Discord.Net) - used for reading Discord messages
* *Mare* - the one who requested this plugin
