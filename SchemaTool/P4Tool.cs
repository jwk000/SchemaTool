using Monitor.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemaTool
{
    class P4Info
    {
        public string server;
        public string user;
        public string passwd;
        public string client;
    }

    [Flags]
    enum P4InfoFlag
    {
        P4SERVER = 1,
        P4USER = 2,
        P4PASS = 4,
        P4CLIENT = 8,
        P4ALL = 15
    }

    static class P4Tool
    {
        static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        static P4Info m_p4info;
        public static bool ReadP4Info(string configpath)
        {
            if (!File.Exists(configpath))
            {
                logger.Error($"没有找到p4配置文件{configpath}，跳过p4检查");
                return false;
            }
            string[] lines = File.ReadAllLines(configpath);
            P4Info info = new P4Info();
            P4InfoFlag flag = 0;
            foreach (string line in lines)
            {
                string[] args = line.Split(':');
                if (args[0] == "Server")
                {
                    info.server = args[1] + ":" + args[2];
                    flag |= P4InfoFlag.P4SERVER;
                }
                if (args[0] == "User")
                {
                    info.user = args[1];
                    flag |= P4InfoFlag.P4USER;
                }
                if (args[0] == "Password")
                {
                    info.passwd = args[1];
                    flag |= P4InfoFlag.P4PASS;
                }
                if (args[0] == "Workspace")
                {
                    info.client = args[1];
                    flag |= P4InfoFlag.P4CLIENT;
                }
            }
            if (flag == P4InfoFlag.P4ALL)
            {
                m_p4info = info;
                return true;
            }
            return false;
        }

        //linkbase是链接目录的路径
        public static void PerforceReconcile(string exportpath, string linkbase=null)
        {
            if (m_p4info == null)
            {
                return;
            }
            if (JunctionPoint.Exists(linkbase))
            {
                string replace = JunctionPoint.GetTarget(linkbase);
                exportpath = exportpath.Replace(linkbase, replace);
            }
            exportpath=exportpath.Replace('\\', '/');
            if (exportpath.EndsWith("/"))
            {
                exportpath = exportpath.Substring(0, exportpath.Length - 1);
            }
            Process p4 = new Process();
            p4.StartInfo.FileName = "p4.exe";
            p4.StartInfo.Arguments = $"-p {m_p4info.server} -u {m_p4info.user} -P {m_p4info.passwd} -c {m_p4info.client} reconcile -aedI {exportpath}/...";
            p4.StartInfo.UseShellExecute = false;
            p4.Start();
        }

        public static void TestJunction()
        {
            JunctionPoint.Create("./testjunction", "./cache", true);
            if (JunctionPoint.Exists("../client/PublishResources/config/"))
            {
                string src = JunctionPoint.GetTarget("../client/PublishResources/config/");
            }

        }

    }
}
