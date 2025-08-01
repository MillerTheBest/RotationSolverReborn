using System.ComponentModel;

namespace RebornRotations.Healer;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.25")]
[SourceCode(Path = "main/RebornRotations/Healer/AST_Reborn.cs")]
[Api(5)]
public sealed class AST_Reborn : AstrologianRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Enable Swiftcast Restriction Logic to attempt to prevent actions other than Raise when you have swiftcast")]
    public bool SwiftLogic { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use both stacks of Lightspeed while moving")]
    public bool LightspeedMove { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use GCDs to heal. (Ignored if you are the only healer in party)")]
    public bool GCDHeal { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Prevent actions while you have the bubble mit up")]
    public bool BubbleProtec { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Prioritize Microcosmos over all other healing when available")]
    public bool MicroPrio { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Simple Lord of Crowns logic (use under divinaiton)")]
    public bool SimpleLord { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Detonate Earlthy Star when you have Giant Dominance")]
    public bool StellarNow { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Earthly Star as an attack while moving")]
    public bool StarMove { get; set; } = true;

    [Range(4, 20, ConfigUnitType.Seconds)]
    [RotationConfig(CombatType.PvE, Name = "Use Earthly Star during countdown timer.")]
    public float UseEarthlyStarTime { get; set; } = 4;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Aspected Benefic")]
    public float AspectedBeneficHeal { get; set; } = 0.4f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold among party member needed to use Horoscope")]
    public float HoroscopeHeal { get; set; } = 0.3f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum average HP threshold among party members needed to use Lady Of Crowns")]
    public float LadyOfHeals { get; set; } = 0.8f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Essential Dignity 3rd charge")]
    public float EssentialDignityThird { get; set; } = 0.8f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Essential Dignity 2nd charge")]
    public float EssentialDignitySecond { get; set; } = 0.7f;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Minimum HP threshold party member needs to be to use Essential Dignity last charge")]
    public float EssentialDignityLast { get; set; } = 0.6f;

    [RotationConfig(CombatType.PvE, Name = "Prioritize Essential Dignity over single target GCD heals when available")]
    public EssentialPrioStrategy EssentialPrio2 { get; set; } = EssentialPrioStrategy.UseGCDs;

    public enum EssentialPrioStrategy : byte
    {
        [Description("Ignore setting")]
        UseGCDs,

        [Description("When capped")]
        CappedCharges,

        [Description("Any charges")]
        AnyCharges,
    }
    #endregion

    private static bool InBurstStatus => HasDivination;

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < MaleficPvE.Info.CastTime + CountDownAhead && MaleficPvE.CanUse(out IAction? act))
        {
            return act;
        }

        if (remainTime < 3 && UseBurstMedicine(out act))
        {
            return act;
        }

        if (remainTime is < 4 and > 3 && AspectedBeneficPvE.CanUse(out act))
        {
            return act;
        }

        if (remainTime < UseEarthlyStarTime && EarthlyStarPvE.CanUse(out act, skipTTKCheck: true))
        {
            return act;
        }

        return remainTime < 30 && AstralDrawPvE.CanUse(out act) ? act : base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (BubbleProtec && HasCollectiveUnconscious)
        {
            return false;
        }

        if (MicroPrio && HasMacrocosmos)
        {
            return false;
        }

        if (!InCombat)
        {
            return false;
        }

        if (OraclePvE.CanUse(out act))
        {
            return true;
        }

        if (nextGCD.IsTheSameTo(false, HeliosConjunctionPvE, AspectedHeliosPvE))
        {
            if (NeutralSectPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (nextGCD.IsTheSameTo(false, HeliosConjunctionPvE, HeliosPvE))
        {
            if (HoroscopePvE.CanUse(out act))
            {
                return true;
            }
        }

        if (nextGCD.IsTheSameTo(true, BeneficPvE, BeneficIiPvE, AspectedBeneficPvE))
        {
            if (SynastryPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (DivinationPvE.CanUse(out _) && UseBurstMedicine(out act))
        {
            return true;
        }

        if (StellarNow && HasGiantDominance && StellarDetonationPvE.CanUse(out act))
        {
            return true;
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ExaltationPvE, ActionID.TheArrowPvE, ActionID.TheSpirePvE, ActionID.TheBolePvE, ActionID.TheEwerPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (BubbleProtec && HasCollectiveUnconscious)
        {
            return false;
        }

        if (InCombat && TheSpirePvE.CanUse(out act))
        {
            return true;
        }

        if (InCombat && TheBolePvE.CanUse(out act))
        {
            return true;
        }

        if (ExaltationPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.CollectiveUnconsciousPvE, ActionID.SunSignPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (BubbleProtec && HasCollectiveUnconscious)
        {
            return false;
        }

        if (SunSignPvE.CanUse(out act))
        {
            return true;
        }

        if (EarthlyStarPvE.CanUse(out act))
        {
            return true;
        }

        if ((MacrocosmosPvE.Cooldown.IsCoolingDown && !MacrocosmosPvE.Cooldown.WillHaveOneCharge(150))
            || (CollectiveUnconsciousPvE.Cooldown.IsCoolingDown && !CollectiveUnconsciousPvE.Cooldown.WillHaveOneCharge(40)))
        {
            return false;
        }

        if (CollectiveUnconsciousPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.TheArrowPvE, ActionID.TheEwerPvE, ActionID.EssentialDignityPvE, ActionID.CelestialIntersectionPvE)]
    protected override bool HealSingleAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (BubbleProtec && HasCollectiveUnconscious)
        {
            return false;
        }

        if (MicroPrio && HasMacrocosmos)
        {
            return false;
        }

        if (InCombat && TheArrowPvE.CanUse(out act))
        {
            return true;
        }

        if (InCombat && TheEwerPvE.CanUse(out act))
        {
            return true;
        }

        if (EssentialDignityPvE.Cooldown.CurrentCharges == 3 && EssentialDignityPvE.CanUse(out act, usedUp: true) && EssentialDignityPvE.Target.Target?.GetHealthRatio() < EssentialDignityThird)
        {
            return true;
        }

        if (EssentialDignityPvE.Cooldown.CurrentCharges == 2 && EssentialDignityPvE.CanUse(out act, usedUp: true) && EssentialDignityPvE.Target.Target?.GetHealthRatio() < EssentialDignitySecond)
        {
            return true;
        }

        if (EssentialDignityPvE.Cooldown.CurrentCharges == 1 && EssentialDignityPvE.CanUse(out act, usedUp: true) && EssentialDignityPvE.Target.Target?.GetHealthRatio() < EssentialDignityLast)
        {
            return true;
        }

        if (CelestialIntersectionPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        return base.HealSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.CelestialOppositionPvE, ActionID.StellarDetonationPvE, ActionID.HoroscopePvE, ActionID.HoroscopePvE_16558, ActionID.LadyOfCrownsPvE)]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (BubbleProtec && HasCollectiveUnconscious)
        {
            return false;
        }

        if (HasGiantDominance && StellarDetonationPvE.CanUse(out act))
        {
            return true;
        }

        if (MicrocosmosPvE.CanUse(out act))
        {
            return true;
        }

        if (MicroPrio && HasMacrocosmos)
        {
            return false;
        }

        if (CelestialOppositionPvE.CanUse(out act))
        {
            return true;
        }

        if (StellarDetonationPvE.CanUse(out act))
        {
            return true;
        }

        if (HoroscopePvE_16558.CanUse(out act))
        {
            return true;
        }

        if (HoroscopePvE.CanUse(out act))
        {
            return true;
        }

        if (LadyOfCrownsPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealAreaAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (BubbleProtec && HasCollectiveUnconscious)
        {
            return false;
        }

        if (Player.WillStatusEnd(5, true, StatusID.Suntouched))
        {
            if (SunSignPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (PartyMembersAverHP < LadyOfHeals && LadyOfCrownsPvE.CanUse(out act))
        {
            return true;
        }

        if (AstralDrawPvE.Cooldown.WillHaveOneCharge(3) && LadyOfCrownsPvE.CanUse(out act))
        {
            return true;
        }

        if (AstralDrawPvE.Cooldown.WillHaveOneCharge(3) && InCombat && TheEwerPvE.CanUse(out act))
        {
            return true;
        }

        if (AstralDrawPvE.Cooldown.WillHaveOneCharge(3) && InCombat && TheBolePvE.CanUse(out act))
        {
            return true;
        }

        if (UmbralDrawPvE.Cooldown.WillHaveOneCharge(3) && InCombat && TheArrowPvE.CanUse(out act))
        {
            return true;
        }

        if (UmbralDrawPvE.Cooldown.WillHaveOneCharge(3) && InCombat && TheSpirePvE.CanUse(out act))
        {
            return true;
        }

        if (AstralDrawPvE.CanUse(out act))
        {
            return true;
        }

        if (UmbralDrawPvE.CanUse(out act))
        {
            return true;
        }

        if ((HasDivination || !DivinationPvE.Cooldown.WillHaveOneCharge(66) || !DivinationPvE.EnoughLevel) && InCombat && TheBalancePvE.CanUse(out act))
        {
            return true;
        }

        if ((HasDivination || !DivinationPvE.Cooldown.WillHaveOneCharge(66) || !DivinationPvE.EnoughLevel) && InCombat && TheSpearPvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (BubbleProtec && HasCollectiveUnconscious)
        {
            return false;
        }

        if (SimpleLord && InCombat && HasDivination && LordOfCrownsPvE.CanUse(out act))
        {
            return true;
        }

        if (IsBurst && !IsMoving && InCombat && DivinationPvE.CanUse(out act))
        {
            return true;
        }

        if (AstralDrawPvE.CanUse(out act, usedUp: IsBurst))
        {
            return true;
        }

        if (!HasLightspeed && InCombat &&
            (InBurstStatus
            || DivinationPvE.Cooldown.ElapsedAfter(115)
            || DivinationPvE.Cooldown.WillHaveOneCharge(5)
            || HasDivination) && LightspeedPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if (InCombat)
        {
            if (!HasLightspeed && IsMoving && LightspeedPvE.CanUse(out act, usedUp: LightspeedMove))
            {
                return true;
            }

            if (((!StarMove && !IsMoving) || StarMove) && !HasGiantDominance && !HasEarthlyDominance && EarthlyStarPvE.CanUse(out act))
            {
                return true;
            }

            if (!SimpleLord &&
                (HasDivination
                || !DivinationPvE.Cooldown.WillHaveOneCharge(45)
                || !DivinationPvE.EnoughLevel
                || UmbralDrawPvE.Cooldown.WillHaveOneCharge(3)) && LordOfCrownsPvE.CanUse(out act))
            {
                return true;
            }
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    [RotationDesc(ActionID.MacrocosmosPvE)]
    protected override bool DefenseSingleGCD(out IAction? act)
    {
        act = null;
        if (BubbleProtec && HasCollectiveUnconscious)
        {
            return false;
        }

        if ((MacrocosmosPvE.Cooldown.IsCoolingDown && !MacrocosmosPvE.Cooldown.WillHaveOneCharge(150))
            || (CollectiveUnconsciousPvE.Cooldown.IsCoolingDown && !CollectiveUnconsciousPvE.Cooldown.WillHaveOneCharge(40)))
        {
            return false;
        }

        if ((NeutralSectPvE.CanUse(out _) || HasNeutralSect || IsLastAbility(false, NeutralSectPvE)) && AspectedBeneficPvE.CanUse(out act, skipStatusProvideCheck: true))
        {
            return true;
        }

        return base.DefenseAreaGCD(out act);
    }

    [RotationDesc(ActionID.MacrocosmosPvE)]
    protected override bool DefenseAreaGCD(out IAction? act)
    {
        act = null;
        if (BubbleProtec && HasCollectiveUnconscious)
        {
            return false;
        }

        if ((MacrocosmosPvE.Cooldown.IsCoolingDown && !MacrocosmosPvE.Cooldown.WillHaveOneCharge(150))
            || (CollectiveUnconsciousPvE.Cooldown.IsCoolingDown && !CollectiveUnconsciousPvE.Cooldown.WillHaveOneCharge(40)))
        {
            return false;
        }

        if ((NeutralSectPvE.CanUse(out _) || HasNeutralSect || IsLastAbility(false, NeutralSectPvE)) && HeliosConjunctionPvE.CanUse(out act, skipStatusProvideCheck: true))
        {
            return true;
        }

        if (MacrocosmosPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseAreaGCD(out act);
    }

    [RotationDesc(ActionID.AspectedBeneficPvE, ActionID.BeneficIiPvE, ActionID.BeneficPvE)]
    protected override bool HealSingleGCD(out IAction? act)
    {
        act = null;
        if (BubbleProtec && HasCollectiveUnconscious)
        {
            return false;
        }

        if (HasSwift && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
        {
            return false;
        }

        if (MicroPrio && HasMacrocosmos)
        {
            return false;
        }

        if (EssentialPrio2 == EssentialPrioStrategy.AnyCharges && EssentialDignityPvE.EnoughLevel && EssentialDignityPvE.Cooldown.CurrentCharges > 0)
        {
            return false;
        }

        if (EssentialPrio2 == EssentialPrioStrategy.CappedCharges && EssentialDignityPvE.EnoughLevel && EssentialDignityPvE.Cooldown.CurrentCharges == EssentialDignityPvE.Cooldown.MaxCharges)
        {
            return false;
        }

        if (AspectedBeneficPvE.CanUse(out act) && (IsMoving || AspectedBeneficPvE.Target.Target?.GetHealthRatio() < AspectedBeneficHeal))
        {
            return true;
        }

        if (BeneficIiPvE.CanUse(out act))
        {
            return true;
        }

        if (BeneficPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealSingleGCD(out act);
    }

    [RotationDesc(ActionID.AspectedHeliosPvE, ActionID.HeliosPvE, ActionID.HeliosConjunctionPvE)]
    protected override bool HealAreaGCD(out IAction? act)
    {
        act = null;
        if (BubbleProtec && HasCollectiveUnconscious)
        {
            return false;
        }

        if (HasSwift && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
        {
            return false;
        }

        if (MicroPrio && HasMacrocosmos)
        {
            return false;
        }

        if (HeliosConjunctionPvE.CanUse(out act))
        {
            return true;
        }

        if (AspectedHeliosPvE.CanUse(out act))
        {
            return true;
        }

        if (HeliosPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealAreaGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        act = null;
        if (BubbleProtec && HasCollectiveUnconscious)
        {
            return false;
        }

        if (HasSwift && SwiftLogic && MergedStatus.HasFlag(AutoStatus.Raise))
        {
            return false;
        }

        if (GravityPvE.CanUse(out act))
        {
            return true;
        }

        if (CombustIiiPvE.CanUse(out act))
        {
            return true;
        }

        if (CombustIiPvE.CanUse(out act))
        {
            return true;
        }

        if (CombustPvE.CanUse(out act))
        {
            return true;
        }

        if (MaleficPvE.CanUse(out act))
        {
            return true;
        }

        if (CombustIiiPvE.CanUse(out act))
        {
            return true;
        }

        if (CombustIiPvE.CanUse(out act))
        {
            return true;
        }

        if (CombustPvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    public override bool CanHealSingleSpell
    {
        get
        {
            int aliveHealerCount = 0;
            IEnumerable<IBattleChara> healers = PartyMembers.GetJobCategory(JobRole.Healer);
            foreach (IBattleChara h in healers)
            {
                if (!h.IsDead)
                    aliveHealerCount++;
            }

            return base.CanHealSingleSpell && (GCDHeal || aliveHealerCount == 1);
        }
    }
    public override bool CanHealAreaSpell
    {
        get
        {
            int aliveHealerCount = 0;
            IEnumerable<IBattleChara> healers = PartyMembers.GetJobCategory(JobRole.Healer);
            foreach (IBattleChara h in healers)
            {
                if (!h.IsDead)
                    aliveHealerCount++;
            }

            return base.CanHealAreaSpell && (GCDHeal || aliveHealerCount == 1);
        }
    }
    #endregion
}
