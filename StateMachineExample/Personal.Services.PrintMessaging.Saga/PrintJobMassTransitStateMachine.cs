// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrintJobMassTransitStateMachine.cs" company="West Bend">
// Copyright (c) 2019 West Bend
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Personal.Services.PrintMessaging.Saga
{
    using System.Collections.Generic;
    using Automatonymous;
    using Newtonsoft.Json;
    using Personal.Contracts.CommonLog;
    using Personal.Service.Api.Logger;

    public abstract class PrintJobMassTransitStateMachine<T> : MassTransitStateMachine<T> where T : class, SagaStateMachineInstance
    {
        protected void Log<S>(string message, BehaviorContext<T, S> context, IMessageLogger logger)
        {
            if (logger == null)
            {
                logger = new MessageLogger(Settings.ApimURL, Settings.ApimSubscriptionKey);
            }

            if (context == null)
            {
                logger.Log(new Log()
                {
                    Messages = new List<LogMessage>()
                    {
                        new LogMessage()
                        {
                            Category = Constants.Logging.CATEGORY,
                            Severity = SeverityType.Information,
                            Message = message,
                        }
                    }
                });
            }
            else
            {
                logger.Log(new Log()
                {
                    Messages = new List<LogMessage>()
                    {
                        new LogMessage()
                        {
                            Category = Constants.Logging.CATEGORY,
                            Severity = SeverityType.Information,
                            Message = message,
                            LogAttributes = this.BuildAdditionalData<S>(context.Data),
                        }
                    }
                });
           }
        }

        private List<LogAttribute> BuildAdditionalData<TS>(TS @event)
        {
            if (@event == null)
            {
                return new List<LogAttribute>();
            }

            return new List<LogAttribute>()
            {
                new LogAttribute()
                {
                    Key = "Message",
                    Value = JsonConvert.SerializeObject(@event),
                }
            };
        }
    }
}