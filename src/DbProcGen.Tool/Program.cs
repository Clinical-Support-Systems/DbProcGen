using DbProcGen.Tool;
using DbProcGen.Tool.Services;

var console = new ConsoleWriter();
var dispatcher = new CommandDispatcher(console);

return dispatcher.Dispatch(args);
