﻿using MiraAPI.Networking;
using MiraAPI.Utilities;
using Reactor.Networking.Attributes;
using Reactor.Utilities;
using Reactor.Utilities.Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiraAPI.Modifiers;

[RegisterInIl2Cpp]
public class ModifierManager(IntPtr ptr) : MonoBehaviour(ptr)
{
    public List<uint> activeModifiersIds = new();
    public static List<BaseModifier> registeredModifiers = new();

    public static void RegisterModifier(Type modifierType)
    {
        if (!typeof(BaseModifier).IsAssignableFrom(modifierType))
        {
            return;
        }

        BaseModifier newMod = (BaseModifier)Activator.CreateInstance(modifierType);
        newMod.ModifierId = (uint)registeredModifiers.Count;
        registeredModifiers.Add(newMod);
    }


    public bool HasModifier<T>(PlayerControl target) where T : BaseModifier
    {
        uint id = registeredModifiers.Find(mod => mod.GetType() == typeof(T)).ModifierId;
        return target.GetModifierManager().activeModifiersIds.Contains(id);
    }

    public void AddModifier<T>(PlayerControl target) where T : BaseModifier
    {
        uint id = registeredModifiers.Find(mod => mod.GetType() == typeof(T)).ModifierId;
        RpcAddModifier(target, id);
    }

    public void RemoveModifier<T>(PlayerControl target) where T : BaseModifier
    {
        uint id = registeredModifiers.Find(mod => mod.GetType() == typeof(T)).ModifierId;
        if (!activeModifiersIds.Contains(id))
        {
            Logger<MiraApiPlugin>.Error($"Cannot remove modifier {typeof(T).Name} because it is not active.");
            return;
        }

        RpcRemoveModifier(target, id);
    }

    [MethodRpc((uint)MiraRpc.AddModifier)]
    private static void RpcRemoveModifier(PlayerControl target, uint modifierId)
    {
        BaseModifier modifier = registeredModifiers.Find(mod => mod.ModifierId == modifierId);
        if (modifier is null) return;

        modifier.OnDeactivate();
        target.GetModifierManager().activeModifiersIds.Remove(modifierId);
    }

    [MethodRpc((uint)MiraRpc.AddModifier)]
    private static void RpcAddModifier(PlayerControl target, uint modifierId)
    {
        BaseModifier modifier = registeredModifiers.Find(mod => mod.ModifierId == modifierId);
        if (modifier is null) return;

        target.GetModifierManager().activeModifiersIds.Add(modifierId);
        modifier.OnActivate();
    }
}