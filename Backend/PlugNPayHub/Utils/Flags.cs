using System;
using System.Collections.Generic;
using System.Linq;

namespace PlugNPayHub.Utils
{
    public class Flags
    {
        public const string Notifications = "NTFN";

        public static bool IsFlagSet(string flags, string flagToTest)
        {
            if (string.IsNullOrEmpty(flags))
                return false;

            foreach (string f in flags.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.Compare(f, flagToTest, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
            }

            return false;
        }

        private readonly List<string> _flagsList = new List<string>();

        public Flags()
        {
        }

        public Flags(string flags)
        {
            List<string> lst = Parse(flags);
            if (lst != null)
                _flagsList.AddRange(lst);
        }

        public bool IsFlagSet(string flag)
        {
            return _flagsList.Contains(flag, StringComparer.InvariantCultureIgnoreCase);
        }

        public void AddFlag(string flag)
        {
            if (!IsFlagSet(flag))
                _flagsList.Add(flag);
        }

        public void AddFlagsList(string flags)
        {
            List<string> addLst = Parse(flags);
            if (addLst == null) return;
            foreach (string nf in addLst)
            {
                if (!IsFlagSet(nf))
                    _flagsList.Add(nf);
            }
        }

        private static List<string> Parse(string flags)
        {
            return flags?.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public override string ToString()
        {
            return string.Join("|", _flagsList.ToArray());
        }

        public int Count => _flagsList.Count;
    }
}
