using System;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Serializer;
using NzbDrone.Common.TPL;
using NzbDrone.Core.Messaging.Commands.Tracking;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.ProgressMessaging;

namespace NzbDrone.Core.Messaging.Commands
{
    public class CommandExecutor : ICommandExecutor
    {
        private readonly Logger _logger;
        private readonly IServiceFactory _serviceFactory;
        private readonly ITrackCommands _trackCommands;
        private readonly IEventAggregator _eventAggregator;
        private readonly TaskFactory _taskFactory;

        public CommandExecutor(Logger logger, IServiceFactory serviceFactory, ITrackCommands trackCommands, IEventAggregator eventAggregator)
        {
            var scheduler = new LimitedConcurrencyLevelTaskScheduler(3);

            _logger = logger;
            _serviceFactory = serviceFactory;
            _trackCommands = trackCommands;
            _eventAggregator = eventAggregator;
            _taskFactory = new TaskFactory(scheduler);
        }

        public void PublishCommand<TCommand>(TCommand command) where TCommand : Command
        {
            Ensure.That(command, () => command).IsNotNull();

            _logger.Trace("Publishing {0}", command.GetType().Name);

            if (_trackCommands.FindExisting(command) != null)
            {
                _logger.Trace("Command is already in progress: {0}", command.GetType().Name);
                return;
            }

            _trackCommands.Store(command);

            ExecuteCommand<TCommand>(command);
        }

        public void PublishCommand(string commandTypeName)
        {
            PublishCommand(commandTypeName, null);
        }

        public void PublishCommand(string commandTypeName, DateTime? lastExecutionTime)
        {
            dynamic command = GetCommand(commandTypeName);
            command.LastExecutionTime = lastExecutionTime;

            PublishCommand(command);
        }

        public Command PublishCommandAsync<TCommand>(TCommand command) where TCommand : Command
        {
            Ensure.That(command, () => command).IsNotNull();

            _logger.Trace("Publishing {0}", command.GetType().Name);

            var existingCommand = _trackCommands.FindExisting(command);

            if (existingCommand != null)
            {
                _logger.Trace("Command is already in progress: {0}", command.GetType().Name);
                return existingCommand;
            }

            _trackCommands.Store(command);

            // TODO: We should use async await (once we get 4.5) or normal Task Continuations on Command processing to prevent blocking the TaskScheduler.
            //       For now we use TaskCreationOptions 0x10, which is actually .net 4.5 HideScheduler.
            //       This will detach the scheduler from the thread, causing new Task creating in the command to be executed on the ThreadPool, avoiding a deadlock.
            //       Please note that the issue only shows itself on mono because since Microsoft .net implementation supports Task inlining on WaitAll.
            if (Enum.IsDefined(typeof(TaskCreationOptions), (TaskCreationOptions)0x10))
            {
                _taskFactory.StartNew(() => ExecuteCommand<TCommand>(command)
                    , TaskCreationOptions.PreferFairness | (TaskCreationOptions)0x10)
                    .LogExceptions();
            }
            else
            {
                _taskFactory.StartNew(() => ExecuteCommand<TCommand>(command)
                    , TaskCreationOptions.PreferFairness)
                    .LogExceptions();
            }

            return command;
        }

        public Command PublishCommandAsync(string commandTypeName)
        {
            dynamic command = GetCommand(commandTypeName);
            return PublishCommandAsync(command);
        }

        private dynamic GetCommand(string commandTypeName)
        {
            var commandType = _serviceFactory.GetImplementations(typeof(Command))
                .Single(c => c.FullName.Equals(commandTypeName, StringComparison.InvariantCultureIgnoreCase));

            return Json.Deserialize("{}", commandType);
        }

        private void ExecuteCommand<TCommand>(Command command) where TCommand : Command
        {
            var handlerContract = typeof(IExecute<>).MakeGenericType(command.GetType());
            var handler = (IExecute<TCommand>)_serviceFactory.Build(handlerContract);

            _logger.Trace("{0} -> {1}", command.GetType().Name, handler.GetType().Name);

            try
            {
                _trackCommands.Start(command);
                BroadcastCommandUpdate(command);

                if (!MappedDiagnosticsContext.Contains("CommandId") && command.SendUpdatesToClient)
                {
                    MappedDiagnosticsContext.Set("CommandId", command.Id.ToString());
                }

                handler.Execute((TCommand)command);

                if (command.State == CommandStatus.Running)
                {
                    _trackCommands.Completed(command);
                }
            }
            catch (Exception e)
            {
                _trackCommands.Failed(command, e);
                throw;
            }
            finally
            {
                BroadcastCommandUpdate(command);
                _eventAggregator.PublishEvent(new CommandExecutedEvent(command));

                if (MappedDiagnosticsContext.Get("CommandId") == command.Id.ToString())
                {
                    MappedDiagnosticsContext.Remove("CommandId");
                }
            }

            _logger.Trace("{0} <- {1} [{2}]", command.GetType().Name, handler.GetType().Name, command.Runtime.ToString(""));
        }


        private void BroadcastCommandUpdate(Command command)
        {
            if (command.SendUpdatesToClient)
            {
                _eventAggregator.PublishEvent(new CommandUpdatedEvent(command));
            }
        }
    }
}
