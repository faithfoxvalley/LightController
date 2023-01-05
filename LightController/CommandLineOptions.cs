using System.Collections.Generic;
using System.Text;

namespace LightController
{
    public class CommandLineOptions
    {
        private Dictionary<string, Option> options = new Dictionary<string, Option>();
        private string optionString;

        public CommandLineOptions(string[] args)
        {
            StringBuilder sb = new StringBuilder();
            string optionName = null;
            List<string> optionArgs = new List<string>();
            foreach(string arg in args)
            {
                if (arg.Contains(' '))
                    sb.Append('"').Append(arg).Append("\" ");
                else
                    sb.Append(arg).Append(' ');

                if(arg.StartsWith('-'))
                {
                    if(optionName != null)
                        options[optionName] = new Option(optionName, optionArgs.ToArray());
                    if (args.Length > 1)
                        optionName = arg.Substring(1);
                    else
                        optionName = null;
                    optionArgs.Clear();
                }
                else if(optionName != null)
                {
                    optionArgs.Add(arg);
                }
            }

            if (sb.Length > 0)
                sb.Length--;
            optionString = sb.ToString();

            if (optionName != null)
                options[optionName] = new Option(optionName, optionArgs.ToArray());
        }

        public bool HasFlag(string name)
        {
            return options.ContainsKey(name);
        }

        public bool TryGetFlagArg(string name, int index, out string arg)
        {
            if (options.TryGetValue(name, out Option option) && index < option.Args.Length)
            {
                arg = option.Args[index];
                return true;
            }

            arg = null;
            return false;
        }

        public override string ToString()
        {
            return optionString;
        }




        private struct Option
        {
            public string Name;
            public string[] Args;

            public Option(string name, string[] args)
            {
                Name = name;
                Args = args;
            }
        }
    }
}
