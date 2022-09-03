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

        [KSPField(advancedTweakable = false, guiName = "#ludiPart_0035", guiActiveEditor = false, guiActive = true, guiActiveUnfocused = false, isPersistant = false, groupDisplayName = "#ludiPart_0038", groupName = "IInfo")]
        public string liquidDisplayName;
        [KSPField(advancedTweakable = false, guiUnits = " U", guiActiveEditor = false, guiActive = true, guiActiveUnfocused = false, guiName = "#ludiPart_0037", guiFormat = "F2", isPersistant = false, groupDisplayName = "#ludiPart_0038", groupName = "IInfo")]
        public double amtOfGas;
        [KSPField(advancedTweakable = false, guiUnits = " atm", guiActiveEditor = false, guiActive = true, guiActiveUnfocused = false, guiName = "#ludiPart_0036", guiFormat = "F2", isPersistant = false, groupDisplayName = "#ludiPart_0038", groupName = "IInfo")]
        public double gasPressure;
        
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
            part.maxPressure = ConfigInfo.instance.maxPressure;
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
            liquidDisplayName = res.info.displayName;
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
            base.OnLoad(node);
        }
        public override void OnUpdate()
        {
            gasPressure = amtOfGas / (res.maxAmount - res.amount + 0.01d * res.maxAmount - amtOfGas) * part.temperature * .00267988744d;
            double delta = Maths.Clamp((vapPressure - gasPressure - part.staticPressureAtm * 0.01d) * rateMul * Maths.Clamp(TimeWarp.deltaTime * 0.4d, 0, 0.2), -amtOfGas, Math.Min(res.amount, res.maxAmount - amtOfGas));
            amtOfGas = Maths.Clamp(amtOfGas + delta, 0, res.maxAmount * 1.00999 - res.amount);
            res.amount = Maths.Clamp(res.amount - delta, 0d, res.maxAmount);
            part.temperature -= delta * Math.Max(0, enthalpyOfVap - vapPressure * 5d) / part.thermalMass * res.info.density * 10000d;
            double trueGaugePressure = gasPressure - part.staticPressureAtm;
            double calibratedPressure = trueGaugePressure * volumePercentage;

            if (-calibratedPressure > ConfigInfo.instance.maxVacuum * ConfigInfo.instance.warningThreshold)
            {
                WarningMessageDisp.SendMessage(ConfigInfo.warningUP + " " + Math.Round(trueGaugePressure, 2) + " atm", (-calibratedPressure - ConfigInfo.instance.maxVacuum * ConfigInfo.instance.warningThreshold) / (ConfigInfo.instance.maxVacuum * ConfigInfo.instance.invWarningThreshold));

                if (-calibratedPressure > ConfigInfo.instance.maxVacuum)
                {
                    if (Maths.Occurred((-calibratedPressure - ConfigInfo.instance.maxVacuum) * 0.01d))
                    {
                        FlightLogger.fetch.LogEvent(part.partInfo.title + " " + ConfigInfo.explodeLogUP + " " + Math.Round(-trueGaugePressure - ConfigInfo.instance.maxVacuum / volumePercentage, 2) + " atm");
                        part.explosionPotential = 0;
                        part.explode();
                    }
                }
            }
            else if (calibratedPressure > ConfigInfo.instance.maxPressure * ConfigInfo.instance.warningThreshold)
            {
                WarningMessageDisp.SendMessage(ConfigInfo.warningOP + " " + Math.Round(trueGaugePressure, 2) + " atm", (calibratedPressure - ConfigInfo.instance.maxPressure * ConfigInfo.instance.warningThreshold) / (ConfigInfo.instance.maxPressure * ConfigInfo.instance.invWarningThreshold));
                
                if (calibratedPressure > ConfigInfo.instance.maxPressure)
                {
                    if (Maths.Occurred((calibratedPressure - ConfigInfo.instance.maxPressure) * 0.007d))
                    {
                        FlightLogger.fetch.LogEvent(part.partInfo.title + " " + ConfigInfo.explodeLogOP + " " + Math.Round(trueGaugePressure - ConfigInfo.instance.maxPressure / volumePercentage, 2) + " atm");
                        part.explode();
                    }
                }
            }
        }
    }
}
