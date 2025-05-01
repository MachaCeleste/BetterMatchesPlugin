using HarmonyLib;
using Miniscript;
using System;
using System.Linq;
using System.Text.RegularExpressions;

[HarmonyPatch]
class BasicIntrinsicsPatch
{
    [HarmonyPatch(typeof(BasicIntrinsics), "AddInstrinsics")]
    static void Postfix()
    {
        Intrinsic intrinsic6 = Intrinsic.Create("matches");
        intrinsic6.AddParam("self");
        intrinsic6.AddParam("pattern");
        intrinsic6.AddParam("regexOptions", "none");
        intrinsic6.code = delegate (TAC.Context context, Intrinsic.Result partialResult)
        {
            Value var = context.GetVar("self");
            ValString valString = context.GetVar("pattern") as ValString;
            ValString valString2 = context.GetVar("regexOptions") as ValString;
            if (var is ValString && valString != null && valString2 != null)
            {
                string value = ((ValString)var).value;
                if (string.IsNullOrEmpty(valString.value))
                {
                    throw new TypeException("Type Error: 'is_match' pattern can't be empty or null");
                }
                if (!new string[]
                {
                    "none",
                    "i",
                    "m",
                    "s",
                    "n",
                    "x"
                }.Contains(valString2.value))
                {
                    throw new RuntimeException("matches: Invalid regex option");
                }
                TimeSpan matchTimeout = TimeSpan.FromMilliseconds(500.0);
                RegexOptions options = valString2.value.ToRegexOptions();
                try
                {
                    MatchCollection matchCollection = Regex.Matches(value, valString.value, options, matchTimeout);
                    ValList valList = new ValList();
                    foreach (object obj in matchCollection)
                    {
                        Match match = (Match)obj;
                        ValList valList2 = new ValList();
                        foreach (object grp in match.Groups)
                        {
                            valList2.values.Add(new ValString(grp.ToString()));
                        }
                        valList.values.Add(valList2);
                    }
                    return new Intrinsic.Result(valList, true);
                }
                catch (RegexMatchTimeoutException)
                {
                    throw new RuntimeException("matches: operation timeout");
                }
            }
            throw new TypeException("Type Error: 'matches' requires string");
        };
    }
}