﻿using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Stryker.Core;
using Stryker.Core.Logging;
using Stryker.Core.Options;
using System;
using System.IO;
using System.Reflection;

namespace Stryker.CLI
{
    public class StrykerCLI
    {
        private IStrykerRunner _stryker { get; set; }
        private ILogger _logger { get; set; }
        public int ExitCode {get; set; }

        public StrykerCLI(IStrykerRunner stryker)
        {
            _stryker = stryker;
            _logger = ApplicationLogging.LoggerFactory.CreateLogger<StrykerCLI>();
            ExitCode = 0;
        }

        /// <summary>
        /// Analyses the arguments and displays an interface to the user. Kicks off the program.
        /// </summary>
        /// <param name="args">User input</param>
        public int Run(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "StrykerNet",
                FullName = "StrykerNet: Stryker mutator for .Net Core",
                Description = "Stryker mutator for .Net Core",
                ExtendedHelpText = "Welcome to StrykerNet for .Net Core. Run dotnet stryker to kick off a mutation test run"
            };

            var configFilePathParam = CreateOption(app, CLIOptions.ConfigFilePath);
            var reporterParam = CreateOption(app, CLIOptions.Reporter);
            var logConsoleParam = CreateOption(app, CLIOptions.LogLevel);
            var timeoutParam = CreateOption(app, CLIOptions.AdditionalTimeoutMS);
            var exludedMutationsParam = CreateOption(app, CLIOptions.ExcludedMutations);
            var fileLogParam = CreateOption(app, CLIOptions.UseLogLevelFile);
            var projectNameParam = CreateOption(app, CLIOptions.ProjectFileName);
            var maxConcurrentTestRunnersParam = CreateOption(app, CLIOptions.MaxConcurrentTestRunners);
            var thresholdHighParam = CreateOption(app, CLIOptions.ThresholdHigh);
            var thresholdLowParam = CreateOption(app, CLIOptions.ThresholdLow);
            var thresholdBreakParam = CreateOption(app, CLIOptions.ThresholdBreak);

            app.HelpOption("--help | -h | -?");

            app.OnExecute(() => {
                // app started
                var options = new OptionsBuilder().Build(
                    Directory.GetCurrentDirectory(),
                    reporterParam,
                    projectNameParam,
                    timeoutParam,
                    exludedMutationsParam,
                    logConsoleParam,
                    fileLogParam,
                    configFilePathParam,
                    maxConcurrentTestRunnersParam,
                    thresholdHighParam,
                    thresholdLowParam,
                    thresholdBreakParam
                    );
                RunStryker(options);
                return this.ExitCode;
            });
            return app.Execute(args);
        }
        
        private void RunStryker(StrykerOptions options)
        {
            // start with the stryker header
            PrintStykerASCIIName();

            StrykerRunResult results = _stryker.RunMutationTest(options);
            if(!results.isScoreAboveThresholdBreak()) 
            {
                this.HandleBreakingThresholdScore(options, results);
            }
        }

        private void HandleBreakingThresholdScore(StrykerOptions options, StrykerRunResult results) {
            _logger.LogError($@"Final mutation score: {results.mutationScore * 100} under breaking threshold value {options.ThresholdOptions.ThresholdBreak}.
Setting exit code to 1 (failure).
Improve the mutation score or set the `threshold-break` value lower to prevent this error in the future.");
            this.ExitCode = 1;
        }

        private void PrintStrykerASCIILogo()
        {
            Console.WriteLine("");
            Chalk.Yellow("             |STRYKER|              "); Console.WriteLine("");
            Chalk.Yellow("       ~control the mutants~        "); Console.WriteLine("");
            Chalk.Blue("           ..####"); Console.Write("@"); Chalk.Blue("####..            "); Console.WriteLine("");
            Chalk.Blue("        .########"); Console.Write("@"); Chalk.Blue("########.         "); Console.WriteLine("");
            Chalk.Blue("      .#####################.       "); Console.WriteLine("");
            Chalk.Blue("     #########"); Chalk.Yellow("#######"); Chalk.Blue("#########      "); Console.WriteLine("");
            Chalk.Blue("    #########"); Chalk.Yellow("##"); Chalk.Blue("#####"); Chalk.Yellow("##"); Chalk.Blue("#########     "); Console.WriteLine("");
            Chalk.Blue("    #########"); Chalk.Yellow("##"); Chalk.Blue("################     "); Console.WriteLine("");
            Chalk.Blue("    "); Console.Write("@@@"); Chalk.Blue("#######"); Chalk.Yellow("#######"); Chalk.Blue("#######"); Console.Write("@@@"); Chalk.Blue("     "); Console.WriteLine("");
            Chalk.Blue("    ################"); Chalk.Yellow("##"); Chalk.Blue("#########     "); Console.WriteLine("");
            Chalk.Blue("    #########"); Chalk.Yellow("##"); Chalk.Blue("#####"); Chalk.Yellow("##"); Chalk.Blue("#########     "); Console.WriteLine("");
            Chalk.Blue("     #########"); Chalk.Yellow("#######"); Chalk.Blue("#########      "); Console.WriteLine("");
            Chalk.Blue("      '######################'      "); Console.WriteLine("");
            Chalk.Blue("        '########"); Console.Write("@"); Chalk.Blue("#########'        "); Console.WriteLine("");
            Chalk.Blue("            '####"); Console.Write("@"); Chalk.Blue("####'            "); Console.WriteLine("");
        }

        private void PrintStykerASCIIName()
        {
            Console.WriteLine(@"
   _____ _              _               _   _ ______ _______ 
  / ____| |            | |             | \ | |  ____|__   __|
 | (___ | |_ _ __ _   _| | _____ _ __  |  \| | |__     | |   
  \___ \| __| '__| | | | |/ / _ \ '__| | . ` |  __|    | |   
  ____) | |_| |  | |_| |   <  __/ |    | |\  | |____   | |   
 |_____/ \__|_|   \__, |_|\_\___|_| (_)|_| \_|______|  |_|   
                   __/ |                                   
                  |___/                                    
");
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyVersion = assembly.GetName().Version;

            Console.WriteLine($@"
Version {assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build} (beta)
"); 
        }

        /// <summary>
        /// Simplify app option creation to prevent code duplication
        /// </summary>
        private CommandOption CreateOption<T>(CommandLineApplication app, CLIOption<T> option) {
            return app.Option($"{option.ArgumentName} | {option.ArgumentShortName}",
                option.ArgumentDescription,
                CommandOptionType.SingleValue);
        }
    }
}
