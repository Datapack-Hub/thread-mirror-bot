# Help Post Mirror Bot [WIP]

A bot for mirroring all help posts in the Datapack Hub Discord server to the Datapack Hub website.

After starting the program, you can use write `help` in the console to get a list possible commands.

## Development
For Highlighting and debugging, download the `C# Dev Kit` extension pack.
No further setup of the environment is required.
<br>
Providing the bot token can be done in any of the following 3 ways:
- Create a file called `bot_token` in the `secrets` folder with the token inside.
- Provide the token or path to the token file as commandline argument directly. Only used if there is no `bot_token` file.
- If none of the above is done the bot will ask to input a token or path to the token file manually.

After the bot connects to the discord api, it will try to find a github auth token in a file called `github_auth` which also have to be in the `secrets` folder. If it doesn't find one, it asks to input the token manually.
The github token is optional and only needed if generated data should be uploaded to a specific repository.

### Testing:<br>
For quick tests in between run
```
dotnet run [<token> | <path/to/token.txt>]
```

### Config:

A default [bot.cfg](/bot.cfg) is provided. All possible fields can be looked up in the [AppConfig](/src/AppConfig.cs) class.

Syntax:
- Field names have to match the AppConfig classes variable names to be recognised.
- Assigning a single value (`=`).
    - Needs to be on the same line.
- Assigning a list of values (`:`)
    - List entries need be listed below the variable name.
    - List entries need to start with at least 1 whitespace character.
    - If the entry is NOT the last entry, add `,` to the end
- Lines with with `#` as the first non-whitespace character are comments.

## Building
To build an executable, .NET 8.0 has to be installed and run
```
dotnet publish
```

## License
This project is licensed under the [MIT License](/LICENSE).