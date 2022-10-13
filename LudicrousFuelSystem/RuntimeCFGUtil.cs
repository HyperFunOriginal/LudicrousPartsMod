using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP;
using System.IO;
using System.Reflection;
using static UnityEngine.Random;
using static KSP.Localization.Localizer;

namespace LudicrousFuelSystem
{
    public static class Maths
    {
        public static double RNGNormalDist()
        {
            double rng = Range(0f, 1f);
            return rng / (1d - rng * rng);
        }
        public static bool Occurred(double commonness)
        {
            return RNGNormalDist() < commonness;
        }
        public static double Clamp(double a, double b, double c)
        {
            if (a < b) return b;
            if (a > c) return c;
            return a;
        }
    }

    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ConfigInfo : MonoBehaviour
    {
        [Persistent]
        public float electricityConsumptionMultiplier = 1f;
        [Persistent]
        public float maxPressure = 40f;
        [Persistent]
        public float maxVacuum = 25f;
        [Persistent]
        public float warningThreshold = .75f;
        [Persistent]
        public float tankExplosionViolence = .25f;
        [Persistent]
        public float antimatterExplViolence = 4.5f;
        [Persistent]
        public float boilingAudioVolume = 1f;

        public float invWarningThreshold => 1f - warningThreshold;

        public static ConfigInfo instance;
        public static string explodeLogEC;
        public static string warningEC;
        public static string warningAcc;
        public static string warningOP;
        public static string warningUP;
        public static string explodeLogOP;
        public static string explodeLogUP;
        public static string explodeLogAcc;
        public static string consumptionMessage;
        public static string maxAccMessage;
        public static string antimatterTankName;
        public static string liquidContainerName;
        public static string liquidContainerPurpose;
        public static string shrapnelName;

        private void WriteConfigIfNoneExists()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string pluginPath = Uri.UnescapeDataString(uri.Path);
            pluginPath = Path.GetDirectoryName(pluginPath);
            if (!File.Exists(pluginPath + "/../Ludicrous_cfg.cfg"))
            {
                Debug.Log("[LudicrousParts] Generating default Ludicrous_cfg.cfg");
                using (StreamWriter configFile = new StreamWriter(pluginPath + "/../Ludicrous_cfg.cfg"))
                {
                    configFile.WriteLine("// LudicrousParts base part behaviour configuration.  Provides ability set user options. Generates at defaults for stock settings and warnings config.");
                    configFile.WriteLine("Ludicrous_config");
                    configFile.WriteLine("{");
                    configFile.WriteLine("	electricityConsumptionMultiplier = 1.0 // Consumption rate of electric charge for antimatter tanks and other parts.");
                    configFile.WriteLine("	maxPressure = 40.0 // Maximum pressure tolerance of parts that contain liquids in atms.");
                    configFile.WriteLine("	maxVacuum = 25.0 // Maximum vacuum tolerance of parts that contain liquids in atms.");
                    configFile.WriteLine("	warningThreshold = 0.75 // Warning threshold in terms of percentage till maximal tolerable value.");
                    configFile.WriteLine("	tankExplosionViolence = 0.25 // Energy of debris shot out by tank explosions.");
                    configFile.WriteLine("	antimatterExplViolence = 4.5 // Energy of debris shot out by antimatter explosions.");
                    configFile.WriteLine("	boilingAudioVolume = 1.0 // Volume of boiling sound effect.");
                    configFile.WriteLine("}");
                    configFile.Flush();
                    configFile.Close();
                }
            }
        }

        public UrlDir.UrlConfig[] baseConfigs;

        void Awake()
        {
            instance = this;
            DontDestroyOnLoad(this);
            baseConfigs = GameDatabase.Instance.GetConfigs("Ludicrous_config");
            if (baseConfigs.Length == 0)
            {
                WriteConfigIfNoneExists();
                Debug.LogWarning("No Ludicrous_config file found, using defaults");
            }
            else if (baseConfigs.Length > 1)
                Debug.LogWarning("Multiple Ludicrous_config files detected, check your install");
            try
            {
                ConfigNode.LoadObjectFromConfig(this, baseConfigs[0].config);
                Debug.Log("[LudicrousParts Configurations] Configuration Loaded: ");
                Debug.Log("electricityConsumptionMultiplier: " + electricityConsumptionMultiplier);
                Debug.Log("maxPressure: " + maxPressure);
                Debug.Log("maxVacuum: " + maxVacuum);
                warningThreshold = Mathf.Clamp01(warningThreshold);
                Debug.Log("tankExplosionViolence: " + tankExplosionViolence);
                Debug.Log("antimatterExplViolence: " + antimatterExplViolence);
                Debug.Log("boilingAudioVolume: " + boilingAudioVolume);
            }
            catch
            {
                Debug.LogWarning("Error loading config, using defaults");
            }
            explodeLogEC = GetStringByTag("#ludiPart_0020");
            consumptionMessage = GetStringByTag("#ludiPart_0021");
            explodeLogAcc = GetStringByTag("#ludiPart_0022");
            maxAccMessage = GetStringByTag("#ludiPart_0023");
            antimatterTankName = GetStringByTag("#ludiPart_0024");
            liquidContainerName = GetStringByTag("#ludiPart_0025");
            liquidContainerPurpose = GetStringByTag("#ludiPart_0026");
            explodeLogOP = GetStringByTag("#ludiPart_0027");
            explodeLogUP = GetStringByTag("#ludiPart_0028");
            warningEC = GetStringByTag("#ludiPart_0031");
            warningAcc = GetStringByTag("#ludiPart_0032");
            warningOP = GetStringByTag("#ludiPart_0033");
            warningUP = GetStringByTag("#ludiPart_0034");
            shrapnelName = GetStringByTag("#ludiPart_0042");
        }
    }
}
