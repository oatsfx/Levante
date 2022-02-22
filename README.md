# Levante
An open-source Discord bot using [Discord.Net-Labs](https://github.com/Discord-Net-Labs/Discord.Net-Labs) for various Destiny 2 Needs.

Developed by [@OatsFX](https://twitter.com/OatsFX).

## Invite the Official Bot to your Server:
Click [this link to invite the bot](https://discord.com/api/oauth2/authorize?client_id=882303133643047005&permissions=8&scope=applications.commands%20bot)!

## Current Features:
- XP Logging and Wipe Detection
- Most Activity Rotations Updates and Tracking
- Leaderboards for things regarding the above
- Guardian view commands
- Emblem Offers
- Try out an in-game emblem

## Known Issues:
- Nothing to see here, yet.
- We've got a list of Known Issues on the [support Discord server](https://discord.gg/XCyngRkqAa) for the smaller issues.

## Having Issues?
There are a few things you can do if there are any issues with the official bot user:
1. Please make sure your issue is not already a [Known Issue](#known-issues).
2. Create an Issue here on GitHub.
3. Join the [official support server](https://discord.gg/XCyngRkqAa) and shoot a message in the #report channel.

# Stuff for Nerds

## Building this Project:
This project is built using the most recent version of Visual Studio Community 2019 using the C# (8.0) language on the .NET Core 3.1 framework.

## Basic Run Downs of Implementions:
### XP Logging
- We make individual calls, per user, to the [Bungie API](https://github.com/Bungie-net/api) while they are playing Destiny 2 to log their XP gains.
- If their are no XP gains after the base refresh three times in a row, we will assume the player is no longer playing Destiny 2.
- After a user is taken out of active logging, we will save their stats into a JSON file which then will be used for leaderboard commands.

### Reset Tracking:
- We've implemented a "Daily/Weekly Reset" system which changes rotations, like Lost Sectors and Raid Challenges, at the same time the Destiny 2 Resets occur. We can do this because most of Destiny's rotations are set and not random. *Random rotations tracking soon?*
- We have a JSON that stores any potential alerts for a Discord user. If a user wants to be notified of a specific rotation, they can use this.
- We check said JSON and let them know if their desired rotation is active for that day or week.
- This feature does not use the Bungie API.

### Emblem Offers:
- We've implemented a way for players to be notified if an limited time emblem is available. These alerts are manual and added by Bot Staff, appointed by myself.
- After an Offer is added, Staff are prompted if they want to sent the offer as an alert. If yes, the embed will make its way to every channel that has the alerts set up.
- This uses the bungie API for images and the Hash Code.

### Leaderboards:
- We store all of the leaderboard data in its own JSON file and then quick sort it when a command is called.

and more to come...

---

Congratulations, you've made it to the end.
