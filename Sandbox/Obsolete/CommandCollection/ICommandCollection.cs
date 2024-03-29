﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit.Obsolete;

public interface ICommandCollection : IList<CommandDescriptor>, ICollection<CommandDescriptor>, IEnumerable<CommandDescriptor>
{
    public void AddCommand(Type commandType);
}
