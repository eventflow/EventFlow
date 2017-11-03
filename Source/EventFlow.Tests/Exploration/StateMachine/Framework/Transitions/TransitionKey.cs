using System;
using EventFlow.Extensions;
using EventFlow.ValueObjects;

namespace EventFlow.Tests.Exploration.StateMachine.Framework.Transitions
{
    public class TransitionKey : SingleValueObject<string>
    {
        public TransitionKey(Type stateType, Type signalType)
            : base(CreateValue(stateType, signalType))
        {
        }

        private static string CreateValue(Type stateType, Type signalType)
        {
            if (stateType == null) throw new ArgumentNullException(nameof(stateType));
            if (signalType == null) throw new ArgumentNullException(nameof(signalType));

            return $"{stateType.PrettyPrint()}-{signalType.PrettyPrint()}";
        }
    }
}