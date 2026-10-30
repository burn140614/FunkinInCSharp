using Funkin;

CommandLine.HandleInformationalSwitches(args);
CommandLine.Parse(args);

using var game = new FunkinGame();
game.Run();
