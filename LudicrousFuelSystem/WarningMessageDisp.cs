using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using KSP;
using System.Collections;

namespace LudicrousFuelSystem
{
    [KSPAddon(KSPAddon.Startup.FlightEditorAndKSC, false)]
    class WarningMessageDisp : MonoBehaviour
    {
        public static WarningMessageDisp instance;
        static string msg;
        static double severity;

        public static void SendMessage(string message, double severity = 0d)
        {
            if (severity <= 0d)
                severity += UnityEngine.Random.Range(-1f, 0f);
            if (WarningMessageDisp.severity < severity)
            {
                WarningMessageDisp.severity = severity;
                msg = message;
            }
        }
        void Start()
        {
            instance = this;
            msg = "";
            severity = -1d;
            StartCoroutine(SlowUpdate());
        }
        IEnumerator SlowUpdate()
        {
            while (enabled && gameObject != null && this != null)
            {
                if (msg != "")
                {
                    ScreenMessage sc = new ScreenMessage(msg, .7f, ScreenMessageStyle.UPPER_CENTER);
                    sc.color.r = (float)Maths.Clamp(severity * 2d, 0.1d, 1d) - 0.1f;
                    sc.color.g = (float)Maths.Clamp(2d - severity * 2d, 0.1d, 1d) - 0.1f;
                    sc.color.b = .1f;
                    sc.color.a = 1f;
                    ScreenMessages.PostScreenMessage(sc);
                    msg = "";
                    severity = -1d;
                }
                yield return new WaitForSecondsRealtime(0.7f);
            }
        }
    }
}
