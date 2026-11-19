using JBeamTool;

void Run()
{
    var args = Environment.GetCommandLineArgs();
    
    if (args.Length < 2)
    {
        UsageHelper.PrintHelp();
        return;
    }

    var command = args[1];
    var commandArgs = ArgumentParser.Parse(args.Skip(2).ToArray());

    switch (command.ToLower().Trim())
    {
        case "keys":
            CommandHandlers.Run_Keys(commandArgs);
            break;
        case "verify":
            CommandHandlers.Run_Verify(commandArgs);
            break;
        case "info":
            CommandHandlers.Run_Info(commandArgs);
            break;
        case "ast":
            CommandHandlers.Run_Ast(commandArgs);
            break;
        case "dump":
            CommandHandlers.Run_Dump(commandArgs);
            break;
        case "slots2":
            CommandHandlers.Run_Slots2(commandArgs);
            break;
        case "pricelist":
            CommandHandlers.Run_PriceList(commandArgs);
            break;
        default:
            UsageHelper.PrintHelp();
            break;
    }
}

Run();
