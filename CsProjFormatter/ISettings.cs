// Copyright (c) 2022 by Stefan Egli.All rights reserved

namespace CsProjFormatter
{
    public interface ISettings
    {
        string EndOfLine { get; }
        string IndentStyle { get; }
        bool SortEntries { get; }
        int TabWidth { get; }
    }

    public class Settings : ISettings
    {
        public string EndOfLine => "crlf";
        public string IndentStyle => "space";
        public bool SortEntries => true;
        public int TabWidth => 2;
    }
}