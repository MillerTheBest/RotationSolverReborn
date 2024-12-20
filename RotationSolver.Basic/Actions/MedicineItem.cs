﻿using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Actions;

/// <summary>
/// The type of the medicine
/// </summary>
public enum MedicineType : byte
{
    /// <summary>
    /// 
    /// </summary>
    None,

    /// <summary>
    /// 
    /// </summary>
    Strength,

    /// <summary>
    /// 
    /// </summary>
    Dexterity,

    /// <summary>
    /// 
    /// </summary>
    Vitality,

    /// <summary>
    /// 
    /// </summary>
    Intelligence,

    /// <summary>
    /// 
    /// </summary>
    Mind,
}

internal class MedicineItem : BaseItem
{
    public MedicineType Type { get; }

    public MedicineItem(Item item) : base(item)
    {
        Type = _item.Icon switch // used to be Unknown19, not entirely sure if Icon is the correct property to use
        {
            10120 => MedicineType.Strength,
            10140 => MedicineType.Dexterity,
            10160 => MedicineType.Vitality,
            10180 => MedicineType.Intelligence,
            10200 => MedicineType.Mind,
            _ => MedicineType.None,
        };
    }

    protected override bool CanUseThis => DataCenter.RightNowTinctureUseType == TinctureUseType.Anywhere || DataCenter.RightNowTinctureUseType == TinctureUseType.InHighEndDuty;
}
