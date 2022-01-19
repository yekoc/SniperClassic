﻿using R2API;
using RoR2;
using RoR2.Orbs;
using SniperClassic.Modules;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

namespace SniperClassic.Hooks
{
    public class OnHitEnemy
    {
        public OnHitEnemy()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, damageInfo, victim) =>
            {
                CharacterBody victimBody = null;
                bool hadSpotter = false;
                bool hadSpotterScepter = false;
                if (NetworkServer.active && victim)
                {
                    victimBody = victim.GetComponent<CharacterBody>();
                    if (victimBody)
                    {
                        if (victimBody.HasBuff(SniperContent.spotterBuff) || victimBody.HasBuff(SniperContent.spotterScepterBuff))
                        {
                            hadSpotter = true;
                            if (victimBody.HasBuff(SniperContent.spotterScepterBuff))
                            {
                                hadSpotterScepter = true;
                            }
                        }
                    }
                }
                orig(self, damageInfo, victim);
                if (NetworkServer.active && !damageInfo.rejected)
                {
                    bool victimPresent = victimBody && victimBody.healthComponent;
                    bool victimAlive = victimPresent && victimBody.healthComponent.alive;
                    if (damageInfo.HasModdedDamageType(SniperContent.spotterDebuffOnHit))
                    {
                        if (victimAlive && damageInfo.procCoefficient > 0f)
                        {
                            victimBody.AddTimedBuff(SniperContent.spotterStatDebuff, 2f);
                        }
                    }
                    if (hadSpotter)
                    {
                        if (damageInfo.attacker)
                        {
                            CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                            if (attackerBody)
                            {
                                if (damageInfo.procCoefficient > 0f && (damageInfo.damage / attackerBody.damage >= 10f))
                                {
                                    //Spotter Targeting/Recharge controller will apply the cooldown.
                                    if (victimPresent)
                                    {
                                        if (victimAlive)
                                        {
                                            if (victimBody.HasBuff(SniperContent.spotterBuff))
                                            {
                                                victimBody.RemoveBuff(SniperContent.spotterBuff);
                                            }
                                            if (victimBody.HasBuff(SniperContent.spotterScepterBuff))
                                            {
                                                victimBody.RemoveBuff(SniperContent.spotterScepterBuff);
                                            }
                                        }

                                        EnemySpotterReference esr = victim.GetComponent<EnemySpotterReference>();
                                        if (esr.spotterOwner)
                                        {
                                            SpotterRechargeController src = esr.spotterOwner.GetComponent<SpotterRechargeController>();
                                            if (src)
                                            {
                                                src.TriggerSpotter();
                                            }
                                        }
                                    }

                                    LightningOrb spotterLightning = new LightningOrb
                                    {
                                        attacker = damageInfo.attacker,
                                        inflictor = damageInfo.attacker,
                                        damageValue = damageInfo.damage * (hadSpotterScepter ? 1.2f : 0.6f),
                                        procCoefficient = 0.5f,
                                        teamIndex = attackerBody.teamComponent.teamIndex,
                                        isCrit = damageInfo.crit,
                                        procChainMask = damageInfo.procChainMask,
                                        lightningType = LightningOrb.LightningType.Tesla,
                                        damageColorIndex = DamageColorIndex.Nearby,
                                        bouncesRemaining = (hadSpotterScepter ? 1 : 0),
                                        targetsToFindPerBounce = 20 * (hadSpotterScepter ? 2 : 1),
                                        range = 30f,
                                        origin = damageInfo.position,
                                        damageType = (DamageType.SlowOnHit | (hadSpotterScepter ? DamageType.Shock5s : DamageType.Stun1s)),
                                        speed = 120f
                                    };

                                    spotterLightning.bouncedObjects = new List<HealthComponent>();

                                    //SpotterLightningController has stuff that prevents dead enemies from being targeted by lightning.
                                    SpotterLightningController stc = damageInfo.attacker.GetComponent<SpotterLightningController>();
                                    if (stc)
                                    {
                                        stc.QueueLightning(spotterLightning, 0.1f);
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}