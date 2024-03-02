using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine.InputSystem;

namespace CatsArmy.patch;

/// <summary>
/// Patch to modify the behavior of a player.
/// </summary>
[HarmonyPatch(typeof(PlayerControllerB))]
public class PlayerControllerBPatch
{
    /// <summary>
    /// Method called when the player jumps.
    ///
    /// Check the link below for more information about Harmony patches.
    /// Class patches: https://github.com/BepInEx/HarmonyX/wiki/Class-patches
    /// Patch parameters: https://github.com/BepInEx/HarmonyX/wiki/Patch-parameters
    /// </summary>
    /// <param name="__instance">Instance that called the method.</param>
    /// <returns>True if the original method should be called, false otherwise.</returns>
    //[HarmonyPrefix]
    //[HarmonyPatch(nameof(PlayerControllerB.PlayerJump))]
    //private static bool OnPlayerJump(ref PlayerControllerB __instance)
    //{
    //    HUDManager.Instance.AddTextToChatOnServer($"isJumping: {__instance.isJumping}");
    //    // When a player jumps, set isJumping to false to prevent the player from jumping.
    //    __instance.isJumping = false;
    //    return false;
    //}
    public static IEnumerator Prefix_PlayerJump(PlayerControllerB __instance, IEnumerator __results)
    {
        yield return Delay;
        InputAction Jump = GetJump();
        if (!Jump.IsInProgress() || !Jump.IsPressed())//performed == on button up
        {
            __instance.isCrouching = true;
        }
        yield return __results;

    }


    public static bool crouchOnHitGround = false;

    public static bool HitGround = false;
    public static bool IsJumping = false;
    private static readonly ManualLogSource Logger = Plugin.Log;
    private const string Crouch = nameof(Crouch);
    private const string Jump = nameof(Jump);
    private const float delay = 0.035f;
    private static readonly WaitForSeconds Delay = new WaitForSeconds(delay);

    private static PlayerControllerB instance = null;
    private static readonly Expression<Action> expression = () =>
            AccessTools.DeclaredMethod(typeof(PlayerControllerB), nameof(PlayerControllerB.PlayerJump), null, null);
    private static readonly Expression<Action> new_expression = () =>
        AccessTools.DeclaredMethod(typeof(PlayerControllerBPatch), nameof(PlayerControllerBPatch.MyWrapper), null, null);
    private static readonly MethodInfo UnpatchedPlayerJump = SymbolExtensions.GetMethodInfo(expression);
    private static readonly MethodInfo PatchedPlayerJump = SymbolExtensions.GetMethodInfo(new_expression);

    [HarmonyPrefix]
    [HarmonyPatch(nameof(PlayerControllerB.Jump_performed))]
    public static void Prefix_PreformedJump(PlayerControllerB __instance)
    {
        if (__instance.isCrouching)
        {
            __instance.Crouch(false);//always uncrouch
            crouchOnHitGround = true;//tobe determined on landing if needed
        }
    }



    [HarmonyPostfix]
    [HarmonyPatch(nameof(PlayerControllerB.Jump_performed))]
    public static void Postfix_PreformedJump(PlayerControllerB __instance)
    {
        __instance.isCrouching = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(PlayerControllerB.PlayerHitGroundEffects))]
    public static void PlayerHitGroundEffects(PlayerControllerB __instance)
    {
        InputAction Jump = GetJump();
        InputAction Crouch = GetCrouch();
        Logger.LogError($"2 {nameof(Jump.IsInProgress)}: {Jump.IsInProgress()}");
        Logger.LogError($"2 {nameof(Jump.IsPressed)}: {Jump.IsPressed()}");
        if (crouchOnHitGround || Crouch.IsPressed())
        {
            crouchOnHitGround = false;
            if (!Jump.IsPressed() && !__instance.isCrouching)
            {
                __instance.Crouch(true);
            }
        }
    }



    [HarmonyTranspiler]
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Jump_performed))]
    public static IEnumerable<CodeInstruction> Patch(IEnumerable<CodeInstruction> instructions, PlayerControllerB __instance,
        InputAction.CallbackContext context)
    {
        //CodeInstruction codeInstruction = CodeInstruction.Call(() => enumerator());
        //var codeMatch = CodeInstruction.Call(() => __instance.PlayerJump);
        //CodeMatch match = new CodeMatch(codeMatch);
        instance = __instance;
        var found = false;
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(UnpatchedPlayerJump))
            {
                yield return new CodeInstruction(OpCodes.Call, PatchedPlayerJump);
                found = true;
            }
            else
            {
                yield return instruction;
            }
        }
        if (!found)
        {
            Logger.LogError("DIDNT FIND CANNOT PATCH AAAAAAAAA");
        }
    }
    //[HarmonyPrefix]
    //[HarmonyPatch(nameof(PlayerControllerB.PlayerJump))]
    private static IEnumerator MyWrapper()
    {
        //Run prefix?
        if (!instance)
        {
            yield break;
        }
        IEnumerator __result = instance.PlayerJump();
        yield return Delay;
        InputAction Jump = GetJump();

        if (Jump.IsInProgress() && Jump.IsPressed())//performed == on button up
        {
            // Run original enumerator code
            while (__result.MoveNext())
            {
                yield return __result.Current;
            }
        }
        else
        {
            instance.isCrouching = true;
        }
        // Run your postfix
    }

    public static InputAction GetJump()
    {
        return IngamePlayerSettings.Instance.playerInput.actions.FindAction(Jump);
    }

    public static InputAction GetCrouch()
    {
        return IngamePlayerSettings.Instance.playerInput.actions.FindAction(Crouch);
    }

}
public static class Extenion
{
    private const float maxDistance = 0.72f;
    public static bool CanJump(this PlayerControllerB localPlayerController, RaycastHit? ___hit = null)
    {
        bool canJump = !Physics.Raycast(localPlayerController.gameplayCamera.transform.position, Vector3.up, out RaycastHit hit,
            maxDistance, localPlayerController.playersManager.collidersAndRoomMask, QueryTriggerInteraction.Ignore);
        ___hit = hit;
        return canJump;
    }
    public static Coroutine ExecuteAfterFrames(this MonoBehaviour mb, int delay, Action action)
    {
        return mb?.StartCoroutine(ExecuteAfterFramesCoroutine(delay, action));
    }
    public static Coroutine ExecuteAfterSeconds(this MonoBehaviour mb, float delay, Action action)
    {
        return mb?.StartCoroutine(ExecuteAfterSecondsCoroutine(delay, action));
    }

    public static IEnumerator ExecuteAfterFramesCoroutine(this int delay, Action action)
    {
        for (int i = 0; i < delay; i++)
            yield return null;
        action();
    }
    public static IEnumerator ExecuteAfterSecondsCoroutine(this float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action();
    }
}
