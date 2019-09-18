﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Terminal.Gui;
using System.Threading;
using Guit.Plugin;
using System.Composition;

namespace Guit
{
    [Shared]
    [Export]
    class CommandService
    {
        readonly Dictionary<Tuple<int, string>, Lazy<IMenuCommand, MenuCommandMetadata>> commands;
        
        [ImportingConstructor]
        public CommandService([ImportMany] IEnumerable<Lazy<IMenuCommand, MenuCommandMetadata>> commands)
        {
            // TODO: handle duplicate global/local command keys
            this.commands = commands.ToDictionary(
                x => Tuple.Create((int)x.Metadata.HotKey, x.Metadata.Context));
        }

        public Task RunAsync(int hotKey, string context)
        {
            if (!commands.TryGetValue(Tuple.Create(hotKey, context), out var command) &&
                !commands.TryGetValue(Tuple.Create(hotKey, default(string)), out command))
                return Task.CompletedTask;

            return Task.Run(() => ExecuteAsync(command));
        }

        public View GetCommands(ContentView view)
        {
            var commandsView = new View
            {
                Height = 1
            };

            var globals = new View();
            var spacer = new Label("||") { X = Pos.Right(globals), TextAlignment = TextAlignment.Centered };
            var locals = new View { X = Pos.Right(spacer) };

            spacer.Width = Dim.Width(view) - 2 - Dim.Width(globals) - Dim.Width(locals);
            commandsView.Add(globals, spacer, locals);

            View current = new Label("");
            globals.Add(current);
            globals.Width = Dim.Width(current);
            foreach (var command in commands.Values.Where(x => string.IsNullOrEmpty(x.Metadata.Context)).OrderBy(x => x.Metadata.Order))
            {
                current = new Button(command.Metadata.HotKey + " " + command.Metadata.DisplayName)
                {
                    CanFocus = false,
                    X = Pos.Right(current),
                    Clicked = async () => await ExecuteAsync(command),
                };
                globals.Add(current);
                // Not sure why we need to do this... seems like the containing view 
                // width should equal the width of its subviews automatically unless 
                // a different width is specified... 
                globals.Width += Dim.Width(current);
            }

            current = new Label("");
            locals.Add(current);
            locals.Width = Dim.Width(current);
            foreach (var command in commands.Values.Where(x => x.Metadata.Context == view.Context).OrderBy(x => x.Metadata.Order))
            {
                current = new Button(command.Metadata.HotKey + " " + command.Metadata.DisplayName)
                {
                    CanFocus = false,
                    X = Pos.Right(current),
                    Clicked = async () => await ExecuteAsync(command),
                };
                locals.Add(current);
                locals.Width += Dim.Width(current);
            }


            return commandsView;
        }


        async Task ExecuteAsync(Lazy<IMenuCommand, MenuCommandMetadata> command, CancellationToken cancellation = default)
        {
            using (var progress = new ReportStatusProgress(command.Metadata.DisplayName, EventStream.Default))
                await command.Value.ExecuteAsync(cancellation);
        }
    }
}
