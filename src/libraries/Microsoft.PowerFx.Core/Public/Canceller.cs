// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.PowerFx
{
    internal class Canceller
    {
        private readonly List<Action> _cancellationAction;

        public Canceller()
        {
            _cancellationAction = new List<Action>();
        }

        public Canceller(params Action[] cancellationActions)
            : this()
        {
            if (cancellationActions != null)
            {
                foreach (Action cancellationAction in cancellationActions)
                {
                    AddAction(cancellationAction);
                }
            }
        }

        public void AddAction(Action cancellationAction)
        {
            if (cancellationAction != null)
            {
                _cancellationAction.Add(cancellationAction);
            }
        }

        public void ThrowIfCancellationRequested()
        {
            foreach (Action cancellationAction in _cancellationAction)
            {
                cancellationAction();
            }
        }
    }
}
