using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsunaPlugin
{
    class Helper
    {
        public static string[] greetings = new string[] {
            "o/",
            "hello",
            "hi",
            "\\o/",
            "yo"
        };

        public static bool IsMatch(string message, string action)
        {
            var split = message.Split(' ');
            if (action == "hello")
            {
                if (split.Length == 1)
                    return true;

                for (int i = 0; i < split.Length; i++)
			    {
                    if (greetings.Contains(split[i]))
                        return true;
			    }
            }
            return false;
        }
    }
}
