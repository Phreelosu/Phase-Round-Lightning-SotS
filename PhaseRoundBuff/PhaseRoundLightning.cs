﻿using BepInEx;
using BepInEx.Configuration;
using RoR2;
using RoR2.Projectile;
using System;
using R2API.Utils;
using R2API;
using UnityEngine;
using Mono.Cecil;

namespace PhaseRoundLightning
{
    [BepInDependency("com.bepis.r2api")]
    [R2API.Utils.R2APISubmoduleDependency(nameof(LanguageAPI), nameof(ProjectileAPI))]
    [BepInPlugin("com.Moffein.PhaseRoundLightning", "Phase Round Lightning", "1.1.1")]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    public class PhaseRoundLightning : BaseUnityPlugin
    {
        float projDamage, lightningDamage, procCoefficient, radius, resetInterval, attackInterval;
        public void Awake()
        {
            string desc;

            //These are the important values
            projDamage = base.Config.Bind<float>(new ConfigDefinition("General Settings", "Projectile Damage"), 5.2f, new ConfigDescription("How much damage direct hits with Phase Round deals.")).Value;
            lightningDamage = base.Config.Bind<float>(new ConfigDefinition("General Settings", "Damage"), 2.6f, new ConfigDescription("How much damage the lightning deals.")).Value;
            procCoefficient = base.Config.Bind<float>(new ConfigDefinition("General Settings", "Lightning Proc Coefficient"), 0.5f, new ConfigDescription("Affects the effectiveness of procs triggered by the lightning.")).Value;
            
            //These are more niche
            radius = base.Config.Bind<float>(new ConfigDefinition("Lightning Settings", "Radius"), 10f, new ConfigDescription("How far the lightning can reach.")).Value;
            resetInterval = base.Config.Bind<float>(new ConfigDefinition("Lightning Settings", "Reset Interval"), 10f, new ConfigDescription("How often the list of targets hit should be reset. Lower this value to allow the lightning to hit the same target multiple times.")).Value;
            attackInterval = base.Config.Bind<float>(new ConfigDefinition("Lightning Settings", "Attack Interval"), 0.06f, new ConfigDescription("How often a lightning attack should be sent out.")).Value;

            desc = "Fire a piercing round for <style=cIsDamage>" + projDamage.ToString("P0").Replace(" ", "").Replace(",", "") + " damage</style>.";
            desc += " Zaps nearby enemies for <style=cIsDamage>" + lightningDamage.ToString("P0").Replace(" ", "").Replace(",", "") + " damage</style>.";

            LanguageAPI.Add("COMMANDO_SECONDARY_DESCRIPTION", desc);

            EntityStateConfiguration phaseConfig = Resources.Load<EntityStateConfiguration>("entitystateconfigurations/EntityStates.Commando.CommandoWeapon.FireFMJ");
            for (int i = 0; i < phaseConfig.serializedFieldsCollection.serializedFields.Length; i++)
            {
                //Debug.Log(phaseConfig.serializedFieldsCollection.serializedFields[i].fieldName);
                if (phaseConfig.serializedFieldsCollection.serializedFields[i].fieldName == "damageCoefficient")
                {
                    phaseConfig.serializedFieldsCollection.serializedFields[i].fieldValue.stringValue = projDamage.ToString();
                }
                else if (phaseConfig.serializedFieldsCollection.serializedFields[i].fieldName == "projectilePrefab")
                {
                    phaseConfig.serializedFieldsCollection.serializedFields[i].fieldValue.objectValue = BuildProjectilePrefab();
                }
            }
        }

        private GameObject BuildProjectilePrefab()
        {
            GameObject proj = Resources.Load<GameObject>("prefabs/projectiles/fmjramping").InstantiateClone("MoffeinPhaseRoundLightning", true);

            //Add Lightning
            ProjectileProximityBeamController pbc = proj.GetComponent<ProjectileProximityBeamController>();
            if (!pbc)
            {
                pbc = proj.AddComponent<ProjectileProximityBeamController>();
            }
            pbc.attackFireCount = 1;
            pbc.attackInterval = attackInterval;
            pbc.attackRange = radius;
            pbc.listClearInterval = resetInterval;
            pbc.minAngleFilter = 0f;
            pbc.maxAngleFilter = 180f;
            pbc.procCoefficient = procCoefficient;
            pbc.damageCoefficient = lightningDamage / projDamage;
            pbc.bounces = 0;
            pbc.lightningType = RoR2.Orbs.LightningOrb.LightningType.Ukulele;

            //Prevents projectiles from disappearing at long range
            ProjectileSimple ps = proj.GetComponent<ProjectileSimple>();
            ps.lifetime = 10f;

            ProjectileOverlapAttack poa = proj.GetComponent<ProjectileOverlapAttack>();
            poa.onServerHit = null;

            ProjectileAPI.Add(proj);
            return proj;
        }
    }
}