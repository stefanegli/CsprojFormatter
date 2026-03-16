// Copyright (c) 2022 by Stefan Egli.All rights reserved

namespace CsProjFormatter
{
    public interface ISettings
    {
        string EndOfLine { get; }
        char IndentStyle { get; }
        bool SortEntries { get; }
        int TabWidth { get; }
    }

    public class Settings : ISettings
    {
        public string EndOfLine => "\r\n";
        public char IndentStyle => ' ';
        public bool SortEntries => true;
        public int TabWidth => 2;
    }
}
