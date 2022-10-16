using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP;

namespace LudicrousFuelSystem
{
    public class LiquidTank : PartModule
    {
        public double enthalpyOfVap;
        public double boilingPoint;
        public double volatility;
        public string liquidName;
        public double gasPressure;
        public bool loaded;
        public Vector3d acceleration
        {
            get
            {
                Vector3d accelerationCost = vessel.acceleration_immediate - vessel.graviticAcceleration;
                if (!vessel.LandedOrSplashed && (vessel.acceleration_immediate == Vector3d.zero || vessel.packed))
                    accelerationCost = Vector3d.zero;
                return accelerationCost;
            }
        }
        AudioSource boilSound;

        [KSPField(advancedTweakable = false, guiName = "#ludiPart_0035", guiActiveEditor = false, guiActive = true, guiActiveUnfocused = false, isPersistant = false, groupDisplayName = "#ludiPart_0038", groupName = "IInfo")]
        public string liquidDisplayName;
        [KSPField(advancedTweakable = false, guiUnits = " U", guiActiveEditor = false, guiActive = true, guiActiveUnfocused = false, guiName = "#ludiPart_0037", guiFormat = "F2", isPersistant = false, groupDisplayName = "#ludiPart_0038", groupName = "IInfo")]
        public double amtOfGas;
        [KSPField(advancedTweakable = false, guiUnits = " atm", guiActiveEditor = false, guiActive = true, guiActiveUnfocused = false, guiName = "#ludiPart_0036", guiFormat = "F2", isPersistant = false, groupDisplayName = "#ludiPart_0038", groupName = "IInfo")]
        public double netPressure;
        
        double v, w, x;
        double vapPressure => Math.Exp(vaporizationCoeff - (vaporizationCoeff * boilingPoint / part.temperature));
        double vaporizationCoeff
        {
            get
            {
                if (v == 0d)
                {
                    double boilingCoeff = boilingPoint * 0.01;
                    v = 3.5 + 11 * Math.Exp(-0.38 / boilingCoeff) - 0.06 * boilingCoeff + 0.00003 * boilingCoeff * boilingCoeff * boilingCoeff;
                    v /= Math.Sqrt(volatility);
                }
                return v;
            }
        }
        double volumePercentage
        {
            get
            {
                if (w == 0d)
                    w = Math.Min(res.info.volume * res.maxAmount * 0.001d / part.mass, 1.5d);
                return w;
            }
        }
        double rateMul
        {
            get
            {
                if (x == 0d)
                    x = Math.Pow(res.maxAmount, 0.45d) * 0.5d;
                return x;
            }
        }
        
        PartResource r;
        public PartResource res
        {
            get
            {
                if (r == null)
                    r = part.Resources.Get(liquidName);
                return r;
            }
        }
        public override string GetInfo()
        {
            return "\n" + ConfigInfo.liquidContainerPurpose + " " + res.info.displayName;
        }
        public override string GetModuleDisplayName()
        {
            return ConfigInfo.liquidContainerName + " (" + res.info.displayName + ")";
        }
        public override void OnLoad(ConfigNode node)
        {
            if (boilSound == null)
                boilSound = gameObject.AddComponent<AudioSource>();
            boilSound.clip = AudioClipManagers.boilingClip;
            boilSound.maxDistance = 35f;
            boilSound.minDistance = 1f;
            boilSound.ignoreListenerVolume = false;
            boilSound.mute = true;
            loaded = false;

            if (node.HasValue("liquidName"))
                liquidName = node.GetValue("liquidName");
            if (node.HasValue("spEnthalpyOfVap"))
                enthalpyOfVap = double.Parse(node.GetValue("spEnthalpyOfVap"));
            if (node.HasValue("boilingPoint"))
                boilingPoint = Math.Max(0, double.Parse(node.GetValue("boilingPoint")));
            if (node.HasValue("volatility"))
                volatility = Math.Max(.1, double.Parse(node.GetValue("volatility")));
            amtOfGas = 0d;
            if (node.HasValue("amtOfGas"))
                amtOfGas = Maths.Clamp(double.Parse(node.GetValue("amtOfGas")), 0, res.maxAmount);
            if (node.HasValue("loaded"))
                loaded = bool.Parse(node.GetValue("loaded"));

            liquidDisplayName = res.info.displayName;
            part.maxPressure = Math.Min(part.maxPressure, Math.Round(ConfigInfo.instance.maxPressure * 101.325d / volumePercentage));
            base.OnLoad(node);
        }
        public override void OnSave(ConfigNode node)
        {
            if (!node.HasValue("spEnthalpyOfVap"))
                node.AddValue("spEnthalpyOfVap", enthalpyOfVap);
            if (!node.HasValue("boilingPoint"))
                node.AddValue("boilingPoint", boilingPoint);
            if (!node.HasValue("amtOfGas"))
                node.AddValue("amtOfGas", amtOfGas);
            else
                node.SetValue("amtOfGas", amtOfGas);
            if (!node.HasValue("volatility"))
                node.AddValue("volatility", volatility);
            if (!node.HasValue("liquidName"))
                node.AddValue("liquidName", liquidName);
            if (!node.HasValue("loaded"))
                node.AddValue("loaded", loaded);
            base.OnLoad(node);
        }

        void AudioCheck(double boilRate)
        {
            boilRate = Maths.Clamp(boilRate, -amtOfGas, res.amount);
            if (boilSound.clip == null)
            {
                boilSound.clip = AudioClipManagers.boilingClip;
                return;
            }
            boilSound.volume = Mathf.Clamp01((float)boilRate * 0.011f * res.info.volume * ConfigInfo.instance.boilingAudioVolume);
            boilSound.pitch = (float)Math.Pow(Math.Abs(boilRate) / res.maxAmount * 3d + .15d, 0.14d);
            bool audible = boilSound.volume > 0.015f && !PauseMenu.isOpen;
            if (audible && !boilSound.isPlaying)
                boilSound.Play();
            boilSound.mute = !audible;
        }
        public override void OnUpdate()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT)
                return;

            if (!loaded)
                part.temperature = Math.Min(part.temperature, boilingPoint);

            double enthalpy = Math.Max(0, enthalpyOfVap - vapPressure * 5d);
            gasPressure = amtOfGas / (res.maxAmount - res.amount + 0.01d * res.maxAmount - amtOfGas) * part.temperature * .00267988744d;
            double trueRate = (vapPressure - gasPressure - part.staticPressureAtm * 0.01d) * rateMul;
            double delta = Maths.Clamp(trueRate * Maths.Clamp(TimeWarp.deltaTime * 0.36d, 0, 50d / Math.Max(150d, enthalpy)), -amtOfGas, Math.Min(res.amount, res.maxAmount - amtOfGas));

            AudioCheck(trueRate);

            if (PauseMenu.isOpen)
                return;

            loaded = true;
            amtOfGas = Maths.Clamp(amtOfGas + delta, 0, res.maxAmount * 1.00999 - res.amount);
            res.amount = Maths.Clamp(res.amount - delta, 0d, res.maxAmount);
            part.temperature -= delta * enthalpy / part.thermalMass * res.info.density * 10000d;
            netPressure = gasPressure - part.staticPressureAtm;
            double calibratedPressure = netPressure * volumePercentage;
            if (!CheatOptions.NoCrashDamage)
                CheckForFailure(calibratedPressure);
        }

        private void CheckForFailure(double calibratedPressure)
        {
            if (-calibratedPressure > ConfigInfo.instance.maxVacuum * ConfigInfo.instance.warningThreshold)
            {
                WarningMessageDisp.SendMessage(ConfigInfo.warningUP + " " + Math.Round(netPressure, 2) + " atm", (-calibratedPressure - ConfigInfo.instance.maxVacuum * ConfigInfo.instance.warningThreshold) / (ConfigInfo.instance.maxVacuum * ConfigInfo.instance.invWarningThreshold));

                if (-calibratedPressure > ConfigInfo.instance.maxVacuum)
                {
                    if (Maths.Occurred((-calibratedPressure - ConfigInfo.instance.maxVacuum) * 0.01d))
                    {
                        FlightLogger.fetch.LogEvent(part.partInfo.title + " " + ConfigInfo.explodeLogUP + " " + Math.Round(-netPressure - ConfigInfo.instance.maxVacuum / volumePercentage, 2) + " atm");
                        part.explosionPotential = 0;
                        part.explode();
                    }
                }
            }
            else if (calibratedPressure > ConfigInfo.instance.maxPressure * ConfigInfo.instance.warningThreshold)
            {
                WarningMessageDisp.SendMessage(ConfigInfo.warningOP + " " + Math.Round(netPressure, 2) + " atm", (calibratedPressure - ConfigInfo.instance.maxPressure * ConfigInfo.instance.warningThreshold) / (ConfigInfo.instance.maxPressure * ConfigInfo.instance.invWarningThreshold));

                if (calibratedPressure > ConfigInfo.instance.maxPressure)
                {
                    if (Maths.Occurred((calibratedPressure - ConfigInfo.instance.maxPressure) * 0.007d))
                    {
                        FlightLogger.fetch.LogEvent(part.partInfo.title + " " + ConfigInfo.explodeLogOP + " " + Math.Round(netPressure - ConfigInfo.instance.maxPressure / volumePercentage, 2) + " atm");
                        ShrapnelGeneration.SpawnShrapnel(part, 20, (float)(calibratedPressure * (res.amount + amtOfGas) * res.info.density) * ConfigInfo.instance.tankExplosionViolence);
                        part.explode();
                    }
                }
            }
        }
    }
}
