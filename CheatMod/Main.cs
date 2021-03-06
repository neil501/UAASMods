﻿using System;
using System.IO;
using System.Reflection;
using Fight.Modifier;
using GOAP;
using Harmony12;
using UltimateAdmiral;
using UltimateAdmiral.UI;
using UnityEngine;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager;

namespace UAAS
{
    internal static class Main
    {
        public static ModEntry mod;
        public static Settings _settings;

        private static bool Load(UnityModManager.ModEntry modEntry)
        {
            mod = modEntry;
            var harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            _settings = Settings.Load<Settings>(modEntry);
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            return true;
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            _settings.Draw(modEntry);
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            _settings.Save(modEntry);
        }

        #region General

        [HarmonyPatch(typeof(UltimateAdmiral.UI.BattleRightBottomPanel))]
        [HarmonyPatch("OnButtonClick")]
        private class SpeedMod
        {
            private static void Postfix(UltimateAdmiral.UI.BattleRightBottomPanel __instance, GameObject selection)
            {
                for (int i = __instance.speedButtons.Length - 1; i > 0; i--)
                {
                    BattleRightBottomPanel.ButtonRec buttonRec = __instance.speedButtons[i];
                    if (buttonRec.selection == selection)
                    {
                        BattleLogic.instance.pauseSound = true;
                        BattleLogic.instance.GameSpeed = buttonRec.speed * 5f * _settings.SpeedMultiplier;
                        return;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PayElement))]
        [HarmonyPatch("Pay")]
        private class Pay
        {
            private static void Prefix(PayElement __instance)
            {
                var instance = PlayerController.instance;
                if (_settings.MinimumReputation > 0)
                {
                    instance.Reputation = Math.Max(_settings.MinimumReputation, instance.Reputation);
                }
                if (_settings.MinimumCash > 0)
                {
                    instance.Gold = Math.Max(_settings.MinimumCash, instance.Gold);
                }
            }
        }

        [HarmonyPatch(typeof(UltimateAdmiral.UI.PaySection))]
        [HarmonyPatch("Pay")]
        private class PaySectionPay
        {
            private static void Prefix(PaySection __instance)
            {
                var instance = PlayerController.instance;
                if (_settings.MinimumReputation > 0)
                {
                    instance.Reputation = Math.Max(_settings.MinimumReputation, instance.Reputation);
                }
                if (_settings.MinimumCash > 0)
                {
                    instance.Gold = Math.Max(_settings.MinimumCash, instance.Gold);
                }
            }
        }

        [HarmonyPatch(typeof(AdmiraltyManager))]
        [HarmonyPatch("PerformPurchaseItem")]
        private class PerformPurchaseItem
        {
            private static void Prefix(AdmiraltyManager __instance)
            {
                if (_settings.InfRifleStock || _settings.InfRifleStockArmory)
                {
                    EPickerContent epickerContent = __instance.SelectedItem.ContentType;
                    if (epickerContent != EPickerContent.Cannon)
                    {
                        if (epickerContent == EPickerContent.Rifle)
                        {
                            StoreRecord storeRecord = (__instance.SelectedItem.ContentItem as RifleItem).StoreRecord;
                            if (_settings.InfRifleStock)
                            {
                                storeRecord.Shop = 999999;
                            }
                            if (_settings.InfRifleStockArmory)
                            {
                                storeRecord.Armory = 999999;
                            }
                        }
                    }
                    else
                    {
                        CannonModule cannon = (__instance.SelectedItem.ContentItem as CannonItem).Cannon;
                        StoreModule storeCannon = PlayerController.instance.GetStoreCannon(cannon, true);
                        if (_settings.InfRifleStock)
                        {
                            storeCannon.Shop = 999999;
                        }
                        if (_settings.InfRifleStockArmory)
                        {
                            storeCannon.Armory = 999999;
                        }
                    }
                }
            }
        }

        #endregion General

        #region Naval

        [HarmonyPatch(typeof(ShipModel.Crew))]
        [HarmonyPatch("ChangeMorale")]
        private class ChangeMorale
        {
            private static void Prefix(ShipModel.Crew __instance, ref float change)
            {
                if (_settings.MaxNavalMorale && __instance.ship.side == RuntimeVars.playerSide)
                {
                    change = 1f;
                }
            }
        }

        [HarmonyPatch(typeof(ShipModel.Crew))]
        [HarmonyPatch("UpdateMorale")]
        private class UpdateMorale
        {
            private static void Prefix(ShipModel.Crew __instance, ref float dt)
            {
                if (_settings.MaxNavalMorale && __instance.ship.side == RuntimeVars.playerSide)
                {
                    dt = 1000f;
                }
            }
        }

        [HarmonyPatch(typeof(ShipModel.Crew))]
        [HarmonyPatch("UpdateStamina")]
        private class UpdateStamina
        {
            private static void Prefix(ShipModel.Crew __instance, ref float drain)
            {
                if (_settings.MaxNavalCondition && __instance.ship.side == RuntimeVars.playerSide)
                {
                    drain = -1000f;
                }
            }
        }

        [HarmonyPatch(typeof(ShipModel))]
        [HarmonyPatch("RecalcAttributes")]
        private class NavalRecalcAttributes
        {
            private static void Prefix(ShipModel __instance)
            {
                if (_settings.MaxNavalCondition && __instance.side == RuntimeVars.playerSide)
                {
                    __instance.crew.Stamina = 1f;
                }
                if (_settings.MaxNavalMorale && __instance.side == RuntimeVars.playerSide)
                {
                    __instance.crew.Morale = 1f;
                }
            }

            private static void Postfix(ShipModel __instance)
            {
                if (_settings.MaxNavalCondition && __instance.side == RuntimeVars.playerSide)
                {
                    __instance.actualAttributes.endurance = 100f;
                    __instance.currentAttributes.endurance = 100f;
                }
                if (_settings.MaxNavalMorale && __instance.side == RuntimeVars.playerSide)
                {
                    __instance.actualAttributes.willpower = 100f;
                    __instance.currentAttributes.willpower = 100f;
                }
            }
        }

        [HarmonyPatch(typeof(NavalController))]
        [HarmonyPatch("CreateShipModel")]
        private class CreateShipModel
        {

            private static void Postfix(NavalController __instance, ShipModel __result)
            {
                if (__result.side == RuntimeVars.playerSide)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        var officer = __result.crew.GetCrewOfficer((ENavalTask)i);
                        if (officer == null)
                            return;
                        officer.attributes.perception *= _settings.OfficerFirepowerModifier;
                        officer.attributes.intelligence *= _settings.OfficerEfficiencyModifier;
                        officer.attributes.strength *= _settings.OfficerMeleeModifier;
                        officer.attributes.dexterity *= _settings.OfficerSailingModifier;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ShipViewController))]
        [HarmonyPatch("UpdateKinematicForces")]
        private class UpdateKinematicForces
        {

            private static void Prefix(ShipViewController __instance, ref ShipModel ___model, ref ShipViewConfig ___viewConfig)
            {
                if (___model.side == RuntimeVars.playerSide)
                {
                    ___viewConfig.forwardForceModifier = _settings.NavalSpeedModifier;
                    ___viewConfig.backwardForceModifier = _settings.NavalSpeedModifier;


                }
            }
        }

        #endregion Naval

        #region Land

        [HarmonyPatch(typeof(UnitModel))]
        [HarmonyPatch("ApplyDamage")]
        private class ApplyDamage
        {
            private static void Prefix(UnitModel __instance)
            {
                if (_settings.NoDamage && __instance.side == RuntimeVars.playerSide)
                    __instance.currentFrameData.damage = 0.0f;
            }
        }

        [HarmonyPatch(typeof(UnitModel))]
        [HarmonyPatch("ShootParamsUpdate")]
        private class ShootParamsUpdate
        {
            private static void Postfix(UnitModel __instance)
            {
                if (_settings.MaxSupply && __instance.side == RuntimeVars.playerSide)
                    __instance.ammunitionCurrentCost = __instance.ammunitionTotalCost;
            }
        }

        [HarmonyPatch(typeof(Fight.Modifier.Morale))]
        [HarmonyPatch("Update")]
        private class MoraleUpdate
        {
            private static void Postfix(Morale __instance, ref UnitModel ___data)
            {
                if (_settings.MaxMorale && ___data.side == RuntimeVars.playerSide)
                {
                    __instance.morale = 1f;
                    ___data.morale = 1f;
                }
            }
        }

        [HarmonyPatch(typeof(Fatigue))]
        [HarmonyPatch("Update")]
        private class FatigueUpdate
        {
            private static void Prefix(Fatigue __instance, ref UnitModel ___data, ref float dt)
            {
                if (_settings.MaxCondition && ___data.side == RuntimeVars.playerSide)
                {
                    dt = 10000f;
                }
            }

            private static void Postfix(Fatigue __instance, ref UnitModel ___data)
            {
                if (_settings.MaxCondition && ___data.side == RuntimeVars.playerSide)
                {
                    ___data.condition = 1f;
                }
            }
        }

        [HarmonyPatch(typeof(UnitManager))]
        [HarmonyPatch("CreateUnit")]
        private class UnitManager_StatModifier
        {

            private static void Postfix(UnitManager __instance, ref UnitModel __result)
            {
                if (__result.side == RuntimeVars.playerSide)
                {
                    __result.maxSpeed *= _settings.SpeedModifier;
                    __result.entity.spottingRange *= _settings.SpottingModifier;
                    __result.entity.stealthRange *= _settings.StealthModifier;

                    Attributes[] attributes = new Attributes[3] { __result.startAttributes, __result.actualAttributes, __result.currentAttributes };
                    for (int i = 0; i < attributes.Length; i++)
                    {
                        var attribute = attributes[i];
                        attribute.perception *= _settings.FirepowerModifier;
                        attribute.intelligence *= _settings.EfficiencyModifier;
                        attribute.strength *= _settings.MeleeModifier;
                    }
                    __result.startAttributes = attributes[0];
                    __result.actualAttributes = attributes[1];
                    __result.currentAttributes = attributes[2];

                    if (__result.isValid)
                    {
                        BattlePanel.Instance.UpdateContent(true);
                    }
                }
            }
        }

        //[HarmonyPatch(typeof(UnitModel))]
        //[HarmonyPatch("GetOfficerAttributes", MethodType.StaticConstructor)]
        //private class GetOfficerAttributes_StatModifier
        //{

        //    private static void Postfix(UnitModel __instance, ref Attributes __result)
        //    {
        //        if (__instance.side == RuntimeVars.playerSide)
        //        {
        //            __result.perception *= _settings.FirepowerModifier;
        //            __result.intelligence *= _settings.EfficiencyModifier;
        //            __result.strength *= _settings.MeleeModifier;
        //            __result.dexterity *= _settings.SailingModifier;
        //        }
        //    }
        //}

        #endregion Land
    }

    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        [Header("General Cheats")]
        [Draw("Speed Button Multiplier (Ignores 1x)", Precision = 0, Min = 0), Space(5)]
        public int SpeedMultiplier = 1;

        [Draw("Minimum Reputation (Updates when changed)", Precision = 0, Min = 0), Space(5)]
        public int MinimumReputation = 0;

        [Draw("Minimum Cash (Updates when changed)", Precision = 0, Min = 0), Space(5)]
        public int MinimumCash = 0;

        [Draw("Inf. Rifle/Cannon Stock in Shop (Buy Once to update)"), Space(5)]
        public bool InfRifleStock = false;
        [Draw("Inf. Rifle/Cannon Stock in Armory (Buy Once to update)"), Space(5)]
        public bool InfRifleStockArmory = false;

        [Header("Land Battle Cheats (Player Only)")]
        [Draw("Take No Damage"), Space(5)]
        public bool NoDamage = false;
        [Draw("Max Morale"), Space(5)]
        public bool MaxMorale = false;
        [Draw("Max Condition"), Space(5)]
        public bool MaxCondition = false;
        [Draw("Max Supply"), Space(5)]
        public bool MaxSupply = false;


        [Header("Naval Battle Cheats (Player Only)")]
        [Draw("Max Morale"), Space(5)]
        public bool MaxNavalMorale = false;
        [Draw("Max Condition"), Space(5)]
        public bool MaxNavalCondition = false;
        [Draw("Naval Sailing Speed Modifier", Precision = 1, Min = 1), Space(5)]
        public float NavalSpeedModifier = 1f;

        [Header("Experimental")]
        [Draw("Naval Officer Firepower Modifier", Precision = 1, Min = 0), Space(5)]
        public float OfficerFirepowerModifier = 1f;
        [Draw("Naval Officer Efficiency Modifier", Precision = 1, Min = 0), Space(5)]
        public float OfficerEfficiencyModifier = 1f;
        [Draw("Naval Officer Boarding Modifier", Precision = 1, Min = 0), Space(5)]
        public float OfficerMeleeModifier = 1f;
        [Draw("Naval Officer Sailing Modifier", Precision = 1, Min = 0), Space(5)]
        public float OfficerSailingModifier = 1f;

        [Draw("Land Spotting Modifier", Precision = 1, Min = 1), Space(5)]
        public float SpottingModifier = 1f;
        [Draw("Land Stealth Modifier", Precision = 1, Min = 1), Space(5)]
        public float StealthModifier = 1f;
        [Draw("Land Movement Speed Modifier", Precision = 1, Min = 1), Space(5)]
        public float SpeedModifier = 1f;
        [Draw("Land Firepower Modifier", Precision = 1, Min = 0), Space(5)]
        public float FirepowerModifier = 1f;
        [Draw("Land Efficiency Modifier", Precision = 1, Min = 0), Space(5)]
        public float EfficiencyModifier = 1f;
        [Draw("Land Melee Modifier", Precision = 1, Min = 0), Space(5)]
        public float MeleeModifier = 1f;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public void OnChange()
        {
        }
    }
}