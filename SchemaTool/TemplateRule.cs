using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;

namespace SchemaTool
{
    enum eTemplateRule
    {
        METATEXT,
        IF,
        SWITCH,
        FOREACH,
        SET,
    }


    class RuleMatchInfo
    {
        public eTemplateRule rule_type = eTemplateRule.METATEXT;
        public string match_text_begin;
        public string match_text_end;
        public int match_deepth;

    }
    interface ITemplateRule
    {
        RuleMatchInfo match_info { get; set; }
        List<string> rule_lines { get; set; }
        string rule_param_line { get; set; }
        List<string> Apply(TemplateData data);
    }
    class RuleMetaText : ITemplateRule
    {
        public RuleMatchInfo match_info { get; set; }
        public List<string> rule_lines { get; set; }
        public string rule_param_line { get; set; }
        public List<string> Apply(TemplateData data)
        {
            List<string> result = new List<string>();
            foreach (var line in rule_lines)
            {
                string s = data.ExtendMetaData(line);
                result.Add(s);
            }

            return result;
        }
    }
    class RuleSet : ITemplateRule
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public RuleMatchInfo match_info { get; set; }
        public List<string> rule_lines { get; set; }
        public string rule_param_line { get; set; }
        public List<string> Apply(TemplateData data)
        {
            Regex reg = new Regex(@"@{SET\((.+)\s*=\s*(.+)\)}");
            Match m = reg.Match(rule_param_line);
            if (!m.Success)
            {
                logger.Error("{0}格式错误 {1}", match_info.rule_type, rule_param_line);
                return null;
            }

            string var_name = m.Groups[1].Value;
            string extra = m.Groups[2].Value;
            object var_obj = data.ExtraMetaData(extra);
            data.SetLocalVariant(var_name, var_obj);
            return null;
        }
    }

    class RuleIf : ITemplateRule
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public RuleMatchInfo match_info { get; set; }
        public List<string> rule_lines { get; set; }
        public string rule_param_line { get; set; }

        public bool CheckIfCondition(string cond)
        {
            string[] ss = cond.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);

            if (ss.Length > 1)
            {
                bool ret = false;
                foreach (string s in ss)
                {
                    ret = ret || CheckIfCondition(s);
                    if (ret)
                    {
                        return true;
                    }
                }
                return false;
            }

            ss = cond.Split(new string[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);
            if (ss.Length > 1)
            {
                bool ret = true;
                foreach (string s in ss)
                {
                    ret = ret && CheckIfCondition(s);
                    if (!ret)
                    {
                        return false;
                    }
                }
                return true;

            }

            ss = cond.Split(new string[] { "==" }, StringSplitOptions.RemoveEmptyEntries);
            if (ss.Length == 2)
            {
                return (ss[0]) == (ss[1]);
            }

            ss = cond.Split(new string[] { "!=" }, StringSplitOptions.RemoveEmptyEntries);
            if (ss.Length == 2)
            {
                return (ss[0]) != (ss[1]);
            }

            if (cond == "true") return true;
            if (cond == "false") return false;

            return false;
        }
        public bool CheckIfLine(string line)
        {
            Regex reg = new Regex(@"@{IF\((.+)\)}");
            Match m = reg.Match(line);
            if (!m.Success)
            {
                logger.Error("{0}格式错误 {1}", match_info.rule_type, rule_param_line);
                return false;
            }

            string condition = m.Groups[1].Value;
            return CheckIfCondition(condition);
        }
        public List<string> Apply(TemplateData data)
        {
            string line = data.ExtendMetaData(rule_param_line);
            bool condition_is_true = CheckIfLine(line);
            List<string> extend_lines = new List<string>();
            int if_deepth = 0;
            bool find_else = false; //寻找else
            foreach (string s in rule_lines)
            {
                //跳过嵌套if
                if (Regex.IsMatch(s.Trim(), @"@{IF\((.+)\)}"))
                {
                    if_deepth++;
                }
                if (s.Trim() == @"@{END_IF}")
                {
                    if_deepth--;
                }
                if (if_deepth == 0)
                {

                    if (s.Trim() == "@{ELSE}")
                    {
                        find_else = true;
                        continue;
                    }
                    if (Regex.IsMatch(s, @"@{ELSEIF\(.+\)}"))
                    {
                        if (condition_is_true)
                        {
                            find_else = true;
                        }
                        else
                        {
                            condition_is_true = CheckIfLine(s);
                        }
                        continue;
                    }
                }

                if (condition_is_true)
                {
                    if (find_else) break;
                    extend_lines.Add(s);
                }
                else
                {
                    if (find_else)
                    {
                        extend_lines.Add(s);
                    }
                }
            }

            List<string> result = new List<string>();
            TemplateRuleParser parser = new TemplateRuleParser();
            List<ITemplateRule> extend_rules = parser.Parse(extend_lines);
            foreach (var rule in extend_rules)
            {
                var ss = rule.Apply(data);
                result.AddRange(ss);
            }

            return result;
        }
    }
    class RuleSwitch : ITemplateRule
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public RuleMatchInfo match_info { get; set; }
        public List<string> rule_lines { get; set; }
        public string rule_param_line { get; set; }

        public List<string> Apply(TemplateData data)
        {
            Regex reg = new Regex(@"@{SWITCH\((.+)\)}");
            Match m = reg.Match(rule_param_line);
            if (!m.Success)
            {
                logger.Error("{0}格式错误 {1}", match_info.rule_type, rule_param_line);
                return null;
            }

            string condition = m.Groups[1].Value;
            condition = data.ExtraMetaData(condition) as string;
            Regex regcase = new Regex(@"@{CASE\((.+)\)}");
            bool find_case = false;
            int switch_deepth = 0;
            List<string> extend_lines = new List<string>();
            foreach (string s in rule_lines)
            {
                //跳过嵌套switch
                if (reg.IsMatch(s.Trim()))
                {
                    switch_deepth++;
                }
                if (switch_deepth > 0 && "@{END_SWITCH}" == s.Trim())
                {
                    switch_deepth--;
                }
                if (switch_deepth == 0)
                {

                    if (regcase.IsMatch(s.Trim()))
                    {
                        if (find_case)
                            break;

                        string casee = regcase.Match(s).Groups[1].Value;
                        string[] ss = casee.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                        if (ss.Contains(condition))
                        {
                            find_case = true;
                            continue;
                        }
                    }
                }
                if (find_case)
                {
                    extend_lines.Add(s);
                }
            }

            List<string> result = new List<string>();
            TemplateRuleParser parser = new TemplateRuleParser();
            List<ITemplateRule> extend_rules = parser.Parse(extend_lines);
            foreach (var rule in extend_rules)
            {
                var ss = rule.Apply(data);
                if (ss != null)
                {
                    result.AddRange(ss);
                }
            }

            return result;
        }

    }
    class RuleForeach : ITemplateRule
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public RuleMatchInfo match_info { get; set; }
        public List<string> rule_lines { get; set; }
        public string rule_param_line { get; set; }

        public List<string> Apply(TemplateData data)
        {
            Regex reg = new Regex(@"@{FOREACH\((\w+)\s+IN\s+(.+)\)}");
            Match m = reg.Match(rule_param_line);
            if (!m.Success)
            {
                logger.Error("{0}格式错误 {1}", match_info.rule_type, rule_param_line);
                return null;
            }

            string var_name = m.Groups[1].Value;
            string extra = m.Groups[2].Value;
            object[] var_array = data.ExtraMetaData(extra) as object[];

            List<string> result = new List<string>();
            int index = 0;
            data.SetLocalVariant("ForLength", var_array.Length.ToString());
            data.SetLocalVariant("ForLastIndex", (var_array.Length - 1).ToString());

            foreach (object v in var_array)
            {
                data.SetLocalVariant(var_name, v);
                data.SetLocalVariant("ForIndex", index.ToString());
                index++;
                List<string> res = new List<string>();
                TemplateRuleParser parser = new TemplateRuleParser();
                List<ITemplateRule> extend_rules = parser.Parse(rule_lines);
                foreach (var rule in extend_rules)
                {
                    var ss = rule.Apply(data);
                    if (ss != null)
                        res.AddRange(ss);
                }
                result.AddRange(res);
            }


            return result;
        }

    }
    class TemplateRuleParser
    {
        RuleMatchInfo matchtext = new RuleMatchInfo { rule_type = eTemplateRule.METATEXT };
        List<RuleMatchInfo> matchlist = new List<RuleMatchInfo>();
        public TemplateRuleParser()
        {
            matchlist.Add(new RuleMatchInfo { match_text_begin = @"@{IF\(.+\)}", match_text_end = "@{END_IF}", rule_type = eTemplateRule.IF });
            matchlist.Add(new RuleMatchInfo { match_text_begin = @"@{SWITCH\(.+\)}", match_text_end = "@{END_SWITCH}", rule_type = eTemplateRule.SWITCH });
            matchlist.Add(new RuleMatchInfo { match_text_begin = @"@{FOREACH\(.+\)}", match_text_end = "@{END_FOREACH}", rule_type = eTemplateRule.FOREACH });
            matchlist.Add(new RuleMatchInfo { match_text_begin = @"@{SET\(.+\)}", match_text_end = "@{END_SET}", rule_type = eTemplateRule.SET });
        }
        public ITemplateRule CreateRule(RuleMatchInfo info)
        {
            ITemplateRule rule = null;

            if (info.rule_type == eTemplateRule.METATEXT) rule = new RuleMetaText();
            else if (info.rule_type == eTemplateRule.IF) rule = new RuleIf();
            else if (info.rule_type == eTemplateRule.SWITCH) rule = new RuleSwitch();
            else if (info.rule_type == eTemplateRule.FOREACH) rule = new RuleForeach();
            else if (info.rule_type == eTemplateRule.SET) rule = new RuleSet();

            if (rule != null)
            {
                rule.match_info = info;
                rule.rule_lines = new List<string>();
            }
            return rule;
        }

        public RuleMatchInfo MatchBegin(string line)
        {
            foreach (var match in matchlist)
            {
                Regex reg = new Regex(match.match_text_begin);
                if (reg.IsMatch(line.Trim()))
                {
                    match.match_deepth++;
                    return match;
                }
            }
            return matchtext;
        }
        public bool MatchEnd(string line, RuleMatchInfo matchtxt)
        {
            Regex reg = new Regex(matchtxt.match_text_begin);
            if (reg.IsMatch(line.Trim()))
            {
                matchtxt.match_deepth++;
                return false;
            }

            if (matchtxt.match_text_end == line.Trim())
            {
                matchtxt.match_deepth--;
                if (matchtxt.match_deepth == 0)
                {
                    return true;
                }
            }
            return false;
        }
        public List<ITemplateRule> Parse(List<string> rule_lines)
        {
            List<ITemplateRule> extend_rules = new List<ITemplateRule>();

            RuleMatchInfo info = null;
            ITemplateRule rule = null;
            for (int i = 0; i < rule_lines.Count; i++)
            {
                if (info == null || info.rule_type == eTemplateRule.METATEXT)
                {
                    var match_info = MatchBegin(rule_lines[i]);
                    if (match_info != info)
                    {
                        if (rule != null && rule.rule_lines.Count > 0)
                        {
                            extend_rules.Add(rule);
                        }
                        info = match_info;
                        rule = CreateRule(info);
                        if (info.rule_type != eTemplateRule.METATEXT)
                        {
                            rule.rule_param_line = rule_lines[i].Trim();
                            continue;
                        }
                    }

                    rule.rule_lines.Add(rule_lines[i]);
                    continue;
                }

                if (MatchEnd(rule_lines[i], info))
                {
                    //if (rule.rule_lines.Count > 0)
                    {
                        extend_rules.Add(rule);
                    }
                    info = null;
                    rule = null;
                    continue;
                }

                rule.rule_lines.Add(rule_lines[i]);

            }
            if (rule != null)
            {
                extend_rules.Add(rule);
            }
            return extend_rules;
        }
    }
    class TemplateData
    {
        public Dictionary<string, object> globalVariantDict = new Dictionary<string, object>();
        public Dictionary<string, object> localVariantDict = new Dictionary<string, object>();

        public TemplateData()
        {
        }
        public void SetGlobalVariant(string name, object obj)
        {
            globalVariantDict[name] = obj;
        }

        public void SetLocalVariant(string name, object obj)
        {
            localVariantDict[name] = obj;
        }

        public string GetVariantString(string varname)
        {
            object obj;
            if (localVariantDict.TryGetValue(varname, out obj))
            {
                return obj as string;
            }
            if (globalVariantDict.TryGetValue(varname, out obj))
            {
                return obj as string;
            }
            return null;
        }
        public string GetObjectFieldString(string objname, string fieldname)
        {
            return GetVariantObject(objname, fieldname) as string;
        }
        public object GetVariantObject(string objname, string fieldname)
        {
            object obj = null;
            if (localVariantDict.TryGetValue(objname, out obj))
            {
                PropertyInfo info = obj.GetType().GetRuntimeProperty(fieldname);
                return info.GetValue(obj);
            }
            if (globalVariantDict.TryGetValue(objname, out obj))
            {
                PropertyInfo info = obj.GetType().GetRuntimeProperty(fieldname);
                return info.GetValue(obj);
            }
            return null;

        }

        public object ExtraMetaData(string extra)
        {
            Regex reg = new Regex(@"\${(\w+).(\w+)}");
            var m = reg.Match(extra);
            if (!m.Success)
            {
                return false;
            }
            string objname = m.Groups[1].Value;
            string fieldname = m.Groups[2].Value;

            return GetVariantObject(objname, fieldname);
        }

        public string ExtendMetaData(string line)
        {
            Regex reg = new Regex(@"\${(\w+)}");
            if (reg.IsMatch(line))
            {
                line = reg.Replace(line, m => GetVariantString(m.Groups[1].Value));
            }

            reg = new Regex(@"\${(\w+).(\w+)}");
            if (reg.IsMatch(line))
            {
                line = reg.Replace(line, m => GetObjectFieldString(m.Groups[1].Value, m.Groups[2].Value));
            }

            return line;
        }

    }

}
