using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP;
using System.IO;
using System.Reflection;

namespace LudicrousFuelSystem
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class AudioClipManagers : MonoBehaviour
    {
        public static AudioClip boilingClip;
        public static AudioClip condensationClip;

        void Start()
        {
            boilingClip = GameDatabase.Instance.GetAudioClip("LudicrousParts/Sounds/boil");
            condensationClip = GameDatabase.Instance.GetAudioClip("LudicrousParts/Sounds/condense");
        }
    }
}
