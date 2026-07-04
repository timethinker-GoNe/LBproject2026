using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

namespace DialogueQuests
{

    public class NarrativeTool 
    {
        public static string Translate(string txt)
        {
            // FarmingEngine DialogueLocalizer 연결
            // 키 형식("npc.quest.line")이면 JSON에서 텍스트 조회,
            // 일반 텍스트면 그대로 반환 (하위 호환 유지)
            return FarmingEngine.DialogueLocalizer.Get(txt);
        }

        //Replace all codes like [i:custom_id] in the string
        //[i:custom_id] is for CustomInt
        //[f:custom_id] is for CustomFloat
        //[s:custom_id] is for CustomString
        public static string ReplaceCodes(string txt)
        {
            string regex_str = @"\[\w:\w+:?\w*\]";
            if (Regex.IsMatch(txt, regex_str, RegexOptions.None))
            {
                Regex regex = new Regex(regex_str);
                MatchCollection matches = regex.Matches(txt);
                foreach (Match match in matches)
                {
                    string code = match.Value;
                    string value = GetCodeValue(code);
                    txt = txt.Replace(match.Value, value);
                }
            }
            return txt;
        }

        //Get the value of a single code ex: [i:variable_id]
        public static string GetCodeValue(string code)
        {
            string output = "";

            if (code.Length >= 3 && code.Contains(":"))
            {
                string[] vals = code.Substring(1, code.Length - 2).Split(':');
                string type = vals[0];
                string id = vals[1];

                if (type.ToLower() == "i")
                    output = NarrativeData.Get().GetCustomInt(id).ToString();
                if (type.ToLower() == "f")
                    output = NarrativeData.Get().GetCustomFloat(id).ToString();
                if (type.ToLower() == "s")
                    output = NarrativeData.Get().GetCustomString(id);
                if (type.ToLower() == "a")
                    output = NarrativeData.Get().GetActorValue(id).ToString();

                if (type.ToLower() == "q" && vals.Length == 3)
                {
                    string vid = vals[2];
                    output = NarrativeData.Get().GetQuestValue(id, vid).ToString();
                }
            }

            return output;
        }
    }

}
