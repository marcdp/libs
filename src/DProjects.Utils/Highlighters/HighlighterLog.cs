using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DProjects.Utils.Highlighters { 

    public static class HighlighterLog {
       
        // methods
        public static string Highlight(string log) {
            // Colorizes lines of the form: 2026-05-26T10:45:45Z [Level] Message ...
            if (string.IsNullOrEmpty(log)) return log;
            var result = new System.Text.StringBuilder(log.Length * 2);
            var lines = log.Split('\n');
            for (var l = 0; l < lines.Length; l++) {
                var line = lines[l].TrimEnd('\r');
                if (l > 0) result.Append('\n');
                if (string.IsNullOrEmpty(line)) continue;
                var i = 0;
                var length = line.Length;
                // timestamp: token without spaces at the start
                var tsStart = i;
                while (i < length && !char.IsWhiteSpace(line[i])) i++;
                result.Append(ConsoleUtils.COLOR_BRBLACK).Append(line, tsStart, i - tsStart).Append(ConsoleUtils.ATTRIBUTES_NONE);
                // skip space
                if (i < length && line[i] == ' ') { result.Append(' '); i++; }
                // level: [Level]
                if (i < length && line[i] == '[') {
                    var levelStart = i;
                    i++;
                    while (i < length && line[i] != ']') i++;
                    if (i < length) i++; // consume ']'
                    var levelToken = line.Substring(levelStart, i - levelStart); // e.g. "[Information]"
                    var levelInner = levelToken.Length > 2 ? levelToken.Substring(1, levelToken.Length - 2) : "";
                    string levelColor;
                    if (levelInner.StartsWith("ERR", System.StringComparison.OrdinalIgnoreCase)) {
                        levelColor = ConsoleUtils.COLOR_BRRED;
                    } else if (levelInner.StartsWith("ERR", System.StringComparison.OrdinalIgnoreCase)) {
                        levelColor = ConsoleUtils.COLOR_BRRED;
                    } else if (levelInner.StartsWith("War", System.StringComparison.OrdinalIgnoreCase)) {
                        levelColor = ConsoleUtils.COLOR_BRYELLOW;
                    } else if (levelInner.Equals("Debug", System.StringComparison.OrdinalIgnoreCase) || levelInner.Equals("Trace", System.StringComparison.OrdinalIgnoreCase)) {
                        levelColor = ConsoleUtils.COLOR_BRBLACK;
                    } else {
                        levelColor = ConsoleUtils.COLOR_BRGREEN; // Information
                    }
                    result.Append(levelColor).Append(levelToken).Append(ConsoleUtils.ATTRIBUTES_NONE);
                    // skip space
                    if (i < length && line[i] == ' ') { result.Append(' '); i++; }
                }
                // message: rest of the line
                if (i < length) {
                    result.Append(ConsoleUtils.COLOR_WHITE).Append(line, i, length - i).Append(ConsoleUtils.ATTRIBUTES_NONE);
                }
            }
            return result.ToString();
        }

    }

}