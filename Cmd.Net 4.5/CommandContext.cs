﻿using Cmd.Net.Properties;
using System;
using System.IO;

namespace Cmd.Net
{
    /// <summary>
    /// Implements a <see cref="T:Cmd.Net.Command" /> that executes a child command.
    /// </summary>
    public sealed class CommandContext : Command
    {
        #region Fields

        private readonly CommandCollection _commands = new CommandCollection();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Cmd.Net.CommandContext" /> class using
        /// the specified command name and child commands.
        /// </summary>
        /// <param name="name">The name of a command.</param>
        /// <param name="commands">The collection of child commands.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="name" /> is null.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="name" /> is an empty string (""), or contains one or more invalid characters.</exception>
        /// <remarks>
        /// A <paramref name="name" /> can contain letters, digits and underscore characters.
        /// </remarks>
        public CommandContext(string name, params Command[] commands)
            : base(name)
        {
            if (commands != null)
            {
                foreach (Command command in commands)
                    _commands.Add(command);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Cmd.Net.CommandContext" /> class using
        /// the specified command name, the description and child commands.
        /// </summary>
        /// <param name="name">The name of a command.</param>
        /// <param name="description">The description of a command.</param>
        /// <param name="commands">The collection of child commands.</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="name" /> is null.</exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="name" /> is an empty string (""), or contains one or more invalid characters.</exception>
        /// <remarks>
        /// A <paramref name="name" /> can contain letters, digits and underscore characters.
        /// </remarks>
        public CommandContext(string name, string description, params Command[] commands)
            : base(name, description)
        {
            if (commands != null)
            {
                foreach (Command command in commands)
                    _commands.Add(command);
            }
        }

        #endregion

        #region CommandBase Members

        /// <inheritdoc />
        protected override void ExecuteCore(TextReader input, TextWriter output, TextWriter error, ArgumentEnumerator args)
        {
            Command commandBase = null;
            CommandContext commandContext = this;

            if (!args.MoveNext())
            {
                return;
            }

            CommandContextScope executionScope = CommandContextScope.Current;

            if (executionScope != null && executionScope.CurrentContext != this)
            {
                ThrowHelper.ThrowNotCurrentCommandContextException(Name);
            }

            if (args.CurrentName == string.Empty)
            {
                while (string.CompareOrdinal(args.CurrentValue, "..") == 0)
                {
                    if (executionScope == null)
                    {
                        ThrowHelper.ThrowNoCommandContextExecutionScope(Name);
                    }

                    commandContext = executionScope.PopContext();

                    if (!args.MoveNext())
                    {
                        return;
                    }
                }
            }

            do
            {
                if (args.CurrentName == string.Empty)
                {
                    if (string.Compare(args.CurrentValue, "help", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        commandContext.ExecuteHelp(output, executionScope);
                        return;
                    }

                    commandContext._commands.TryGetCommand(args.CurrentValue, out commandBase);
                    commandContext = commandBase as CommandContext;

                    if (commandContext == null)
                    {
                        break;
                    }

                    if (executionScope != null)
                    {
                        executionScope.PushContext(commandContext);
                    }
                }
                else
                {
                    if (string.CompareOrdinal(args.CurrentName, "?") == 0)
                    {
                        commandContext.ExecuteHelp(output, executionScope); return;
                    }
                    else
                    {
                        ThrowHelper.ThrowUnknownArgumentException(Name, args.CurrentName);
                    }
                }
            }
            while (args.MoveNext());

            if (commandBase == null)
            {
                if (executionScope == null || executionScope.CurrentContext == this)
                {
                    ThrowHelper.ThrowUnknownCommandException(args.CurrentValue);
                }
            }
            else
            {
                args.MoveNext();
                commandBase.Execute(input, output, error, args.ContinueFromCurrent());
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the root context for the current thread.
        /// </summary>
        /// <value>The <see cref="T:Cmd.Net.CommandContext" /> that represents the root command context of the current method.</value>
        public static CommandContext Root
        {
            get
            {
                CommandContextScope executionScope = CommandContextScope.Current;

                if (executionScope == null)
                    return null;

                return executionScope.RootContext;
            }
        }

        /// <summary>
        /// Gets the current context for the current command thread.
        /// </summary>
        /// <value>The <see cref="T:Cmd.Net.CommandContext" /> that represents the current command context of the current method.</value>
        public static CommandContext Current
        {
            get
            {
                CommandContextScope executionScope = CommandContextScope.Current;

                if (executionScope == null)
                    return null;

                return executionScope.CurrentContext;
            }
        }

        /// <summary>
        /// Gets a <see cref="T:Cmd.Net.CommandCollection" /> of child commands of this <see cref="T:Cmd.Net.CommandContext" />.
        /// </summary>
        /// <value>The collection of child commands. The default is an empty collection.</value>
        public CommandCollection Commands
        {
            get { return _commands; }
        }

        #endregion

        #region Private Methods

        private void ExecuteHelp(TextWriter output, CommandContextScope executionScope)
        {
            if (executionScope != null)
            {
                while (executionScope.CurrentContext != this)
                    executionScope.PopContext();
            }

            string description = Description;

            if (description != null)
                output.WriteLine(description);

            if (_commands.Count == 0)
                return;

            output.WriteLine();
            output.WriteLine(Resources.CommandsSection);
            output.WriteLine();

            int descriptionIndent = 0;

            foreach (Command command in _commands)
            {
                int nameLength = command.Name.Length;

                if (descriptionIndent < nameLength)
                    descriptionIndent = nameLength;
            }

            descriptionIndent += ArgumentIndent + DescriptionGap;

            foreach (Command command in _commands)
            {
                int indent;

                for (indent = 0; indent < ArgumentIndent; ++indent)
                    output.Write(' ');

                indent += command.Name.Length;

                output.Write(command.Name);

                if (indent + DescriptionGap > descriptionIndent)
                {
                    indent = 0;
                    output.WriteLine();
                }

                for (; indent < descriptionIndent; ++indent)
                    output.Write(' ');

                output.WriteIndented(command.Description, indent, false);
                output.WriteLine();
            }
        }

        #endregion
    }
}
