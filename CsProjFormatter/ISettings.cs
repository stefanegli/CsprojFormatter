// Copyright (c) 2022 by Stefan Egli.All rights reserved

namespace CsProjFormatter
{
    public interface ISettings
    {
        bool SortEntries { get; }
        string IndentStyle { get; }
        int TabWidth { get; }
        string EndOfLine { get; }
    }

    public class Settings : ISettings
    {
        public bool SortEntries => true;
        public string IndentStyle => "space";
        public int TabWidth => 2;
        public string EndOfLine => "crlf";
    }
}
