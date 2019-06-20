using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlnSearch.Helpers
{
    public static class StringCleaner
    {
        private static readonly Dictionary<string, string> _replaceDict = new Dictionary<string, string>();
        private const string regexEscapes = @"[\a\b\f\n\r\t\v\\]";

    }
}
