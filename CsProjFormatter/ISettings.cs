// Copyright (c) 2022 by Stefan Egli.All rights reserved

namespace CsProjFormatter
{
    public interface ISettings
    {
        bool SortEntries { get; }
    }

    public class Settings : ISettings
    {
        public bool SortEntries => true;
    }
}