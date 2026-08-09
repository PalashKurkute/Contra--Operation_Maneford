using System.Collections.Generic;
using System.Text;

// Splits a line of dialogue into the text to show and the commands
// hidden in it: {pause=0.5}, {shake}, {sfx=roar}.
//
// Each command remembers how many characters are on screen when it
// should run. TextMeshPro tags such as <b> are kept in the text but
// don't count as characters, the same way TextMeshPro counts them.
public static class DialogueText
{
    public struct Command
    {
        // Runs once this many characters are visible
        public int index;

        // "pause", "shake", "sfx"...
        public string name;

        // "0.5", "roar"... (empty when there's no "=")
        public string value;
    }

    public static string Parse(string raw, List<Command> commands)
    {
        commands.Clear();

        if (string.IsNullOrEmpty(raw))
            return "";

        StringBuilder text = new StringBuilder(raw.Length);
        int visible = 0;
        int i = 0;

        while (i < raw.Length)
        {
            char c = raw[i];

            // {command} or {command=value}
            if (c == '{')
            {
                int end = raw.IndexOf('}', i + 1);

                if (end > i)
                {
                    string body = raw.Substring(i + 1, end - i - 1);
                    int equals = body.IndexOf('=');

                    Command command = new Command();
                    command.index = visible;
                    command.name = (equals >= 0 ? body.Substring(0, equals) : body)
                        .Trim().ToLowerInvariant();
                    command.value = equals >= 0 ? body.Substring(equals + 1).Trim() : "";

                    commands.Add(command);

                    i = end + 1;

                    // "return! {pause} After": don't leave two spaces
                    if (i < raw.Length && raw[i] == ' ' &&
                        (text.Length == 0 || text[text.Length - 1] == ' '))
                    {
                        i++;
                    }

                    continue;
                }
            }

            // Rich text tags are copied but take no room
            if (c == '<')
            {
                int end = raw.IndexOf('>', i + 1);

                if (end > i)
                {
                    text.Append(raw, i, end - i + 1);
                    i = end + 1;
                    continue;
                }
            }

            text.Append(c);
            visible++;
            i++;
        }

        return text.ToString();
    }
}
