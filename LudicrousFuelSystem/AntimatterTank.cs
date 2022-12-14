using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP;
using static UnityEngine.Random;

namespace LudicrousFuelSystem
{
    public class AntimatterTank : PartModule
    {
        public double storageEfficiency;
        public double maxMagneticLevStr;
        PartResource ah, ec;
        public override string GetInfo()
        {
            return "\n" + Math.Round(EnergyConsumption(ah.maxAmount) / storageEfficiency * 0.25d, 2) + ConfigInfo.consumptionMessage + " " + Math.Round(storageEfficiency * 100d, 2) + "% " + ConfigInfo.maxAccMessage + " " + Math.Round(maxMagneticLevStr * 40d) + "m/s².";
        }
        public override string GetModuleDisplayName()
        {
            return ConfigInfo.antimatterTankName;
        }
        public double EnergyConsumption(double antimatterAmt) => antimatterAmt * antimatterAmt * 0.0015 * ConfigInfo.instance.electricityConsumptionMultiplier;
        public override void OnLoad(ConfigNode node)
        {
            ah = part.Resources.Get("AntiHydrogen");
            ec = part.Resources.Get("ElectricCharge");
            storageEfficiency = 1d;
            maxMagneticLevStr = 10d;
            if (node.HasValue("storageEfficiency"))
                storageEfficiency = Maths.Clamp(double.Parse(node.GetValue("storageEfficiency")), 0, 1);
            if (node.HasValue("maxMagneticLevStr"))
                maxMagneticLevStr = double.Parse(node.GetValue("maxMagneticLevStr"));

            base.OnLoad(node);
        }
        public override void OnSave(ConfigNode node)
        {
            if (!node.HasValue("storageEfficiency"))
                node.AddValue("storageEfficiency", storageEfficiency);
            if (!node.HasValue("maxMagneticLevStr"))
                node.AddValue("maxMagneticLevStr", maxMagneticLevStr);
            base.OnSave(node);
        }

        public override void OnUpdate()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT || PauseMenu.isOpen || CheatOptions.NoCrashDamage)
                return;

            part.explosionPotential = (float)(ah.amount * .2d + .1d);
            if (ah.amount <= 0.01f)
                return;

            if (ec.amount < ec.maxAmount * ConfigInfo.instance.invWarningThreshold)
                WarningMessageDisp.SendMessage(ConfigInfo.warningEC, 1d - ec.amount / (ec.maxAmount * ConfigInfo.instance.invWarningThreshold));

            double accelerationCost = (vessel.acceleration_immediate - vessel.graviticAcceleration).magnitude * 0.025d;
            if (!vessel.LandedOrSplashed && (vessel.acceleration_immediate == Vector3d.zero || vessel.packed))
                accelerationCost = 0d;

            if (accelerationCost > maxMagneticLevStr * ConfigInfo.instance.warningThreshold)
                WarningMessageDisp.SendMessage(ConfigInfo.warningAcc, (accelerationCost - maxMagneticLevStr * ConfigInfo.instance.warningThreshold) / (maxMagneticLevStr * ConfigInfo.instance.invWarningThreshold));

            double powerConsumption = Maths.Clamp(EnergyConsumption(ah.amount) * TimeWarp.deltaTime * accelerationCost, 0d, ec.maxAmount - 1d);
            if (ec.amount < powerConsumption)
            {
                if (Range(0f, 1f) < 0.1f)
                {
                    FlightLogger.fetch.LogEvent(part.partInfo.title + " " + ConfigInfo.explodeLogEC);
                    double explosionMag = ah.amount * ConfigInfo.instance.antimatterExplViolence;
                    ShrapnelGeneration.SpawnShrapnel(part, (int)(explosionMag * 0.1d), (float)explosionMag);
                    part.explode();
                }
            }
            else if (accelerationCost > maxMagneticLevStr)
            {
                if (Maths.RNGNormalDist() < (accelerationCost - maxMagneticLevStr) * 0.2d)
                {
                    FlightLogger.fetch.LogEvent(part.partInfo.title + " " + ConfigInfo.explodeLogAcc);
                    double explosionMag = ah.amount * ConfigInfo.instance.antimatterExplViolence;
                    ShrapnelGeneration.SpawnShrapnel(part, (int)(explosionMag * 0.1d), (float)explosionMag);
                    part.explode();
                }
            }
            else
            {
                part.RequestResource("ElectricCharge", powerConsumption, ResourceFlowMode.ALL_VESSEL_BALANCE);
                part.temperature += powerConsumption * 0.05d / (part.thermalMass + part.resourceThermalMass);
            }
        }
    }
}
