// This does not work and breaks the game but i dont wanna lose my attempt, this was giga chatgpt
using UnityEngine;
using HarmonyLib;
using System;

using System.Collections.Generic;
using System.Reflection;

using UnityEngine.Video;

[HarmonyPatch(typeof(AvailableElements), "GetRandomAvailable")]
public static class Patch_AvailableElements_GetRandomAvailable
{
    // cache private method info
    static readonly MethodInfo GetAvailableAndIncluded_Method =
        AccessTools.Method(typeof(AvailableElements), "GetAvailableAndIncludedElements");

    static bool Prefix(OnlineLobby.Match match, ref Element[] __result)
    {
        int num = GamePreferences.current.prefs.LastUnlockedIndex;
        bool flag = GamePreferences.current.prefs.IncludeLastUnlocked;
        if (match != null)
        {
            num = match.lastUnlockedIndex;
            flag = match.includeLastUnlocked;
        }

        var args = new object[] { num, flag, null, null };
        GetAvailableAndIncluded_Method.Invoke(null, args);
        var available = (List<Element>)args[2];
        var included = (List<Element>)args[3];

        var pool = new List<Element>(available);
        pool.AddRange(included);

        var selected = new List<Element>();
        while (selected.Count < 5 && pool.Count > 0)
        {
            int i = UnityEngine.Random.Range(0, pool.Count);
            selected.Add(pool[i]);
            pool.RemoveAt(i);
        }

        __result = selected.ToArray();
        return false; // skip original method
    }
}

[HarmonyPatch(typeof(VideoSpellPlayer), "Awake")]
public static class Patch_VideoSpellPlayer_Awake_SetSpellCountsLength
{
    static void Postfix(VideoSpellPlayer __instance)
    {
        try
        {
            if (__instance == null) return;
            if (__instance.spellCounts == null || __instance.spellCounts.Length < 5)
            {
                // Grow to 5 keeping existing values if present
                var old = __instance.spellCounts ?? new int[0];
                var n = Math.Max(5, old.Length);
                var arr = new int[n];
                for (int i = 0; i < old.Length && i < n; i++) arr[i] = old[i];
                for (int i = old.Length; i < n; i++) arr[i] = 0;
                __instance.spellCounts = arr;
            }
        }
        catch (Exception ex) { Debug.LogError($"[BalancePatch] Awake patch error: {ex}"); }
    }
}

[HarmonyPatch(typeof(VideoSpellPlayer), "SlideIn")]
public static class Patch_VideoSpellPlayer_SlideIn_Safe
{
    static bool Prefix(VideoSpellPlayer __instance)
    {
        try
        {
            if (__instance == null) return true;

            __instance.gameObject.SetActive(true);
            __instance.dock.gameObject.SetActive(true);

            int count = __instance.spellIcons.Length;
            VideoClip[] clips = new VideoClip[count];

            for (int i = 0; i < count; i++)
            {
                Element e = VideoSpellPlayer.GetMappedElement(i);
                clips[i] = GameUtility.GetSpellByRoundAndElement(e).video;
            }

            __instance.playVideo.InitClips(clips);
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BalancePatch] SlideIn patch error: {ex}");
            return true;
        }
    }
}

[HarmonyPatch(typeof(VideoSpellPlayer), "DraftSpells")]
public static class Patch_VideoSpellPlayer_DraftSpells_More
{
    static bool Prefix(VideoSpellPlayer __instance)
    {
        try
        {
            if (__instance == null) return true;

            int slots = __instance.spellCounts?.Length ?? 5;
            bool allZero = true;
            for (int i = 0; i < slots; i++)
            {
                if (i < __instance.spellCounts.Length && __instance.spellCounts[i] > 0) { allZero = false; break; }
            }

            if (allZero)
            {
                __instance.spellCounts = new int[slots];
                for (int i = 0; i < slots; i++) __instance.spellCounts[i] = 1;
                __instance.spellCounts[UnityEngine.Random.Range(0, slots)]--;
                for (int i = 0; i < PlayerManager.players.Count - 2; i++)
                {
                    __instance.spellCounts[UnityEngine.Random.Range(0, slots)]++;
                }
            }
            if (Globals.tutorial_manager != null)
            {
                // keep a sensible tutorial distribution for 5 slots
                __instance.spellCounts = new int[slots];
                for (int i = 0; i < slots; i++) __instance.spellCounts[i] = (i < 2) ? 1 : 2;
            }

            for (int j = 0; j < Math.Min(4, __instance.spellCounts.Length); j++)
            {
                // UpdateSpellCount is public; call directly via reflection for safety
                var mi = typeof(VideoSpellPlayer).GetMethod("UpdateSpellCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                mi?.Invoke(__instance, new object[] { j });
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BalancePatch] DraftSpells patch error: {ex}");
            return true;
        }
    }
}

[HarmonyPatch(typeof(VideoSpellPlayer), "ShowDraftLine")]
public static class Patch_VideoSpellPlayer_ShowDraftLine_More
{
    static bool CanHighlight(VideoSpellPlayer vsp)
    {
        if (vsp == null) return false;
        if (vsp.playVideo == null) return false;

        var clipsField = typeof(PlayVideo).GetField("clips",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (clipsField == null) return false;

        var clips = clipsField.GetValue(vsp.playVideo) as VideoClip[];
        return clips != null && clips.Length > 0;
    }

    static bool Prefix(VideoSpellPlayer __instance)
    {
        try
        {
            if (__instance == null) return true;

            int slots = __instance.spellCounts?.Length ?? 5;
            __instance.dock.GetChild(7 + GameUtility.GetRound()).gameObject.SetActive(true);

            for (int i = 0; i < slots; i++)
            {
                if (__instance.spellCounts[i] > 0)
                {
                    __instance.activeSpellIndex = i;
                    break;
                }
            }

            __instance.spellHighlights.GetChild(0).position =
                __instance.spellHighlights.GetChild(1 + __instance.activeSpellIndex).position;

            __instance.spellHighlights.GetChild(0).gameObject.SetActive(true);

            if (CanHighlight(__instance))
            {
                Element e = VideoSpellPlayer.GetMappedElement(__instance.activeSpellIndex);
                int round = GameUtility.GetRound();
                __instance.HighlightSpell(e, round);
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BalancePatch] ShowDraftLine patch error: {ex}");
            return true;
        }
    }
}


[HarmonyPatch(typeof(VideoSpellPlayer), "ChooseRandomSpell")]
public static class Patch_VideoSpellPlayer_ChooseRandomSpell_More
{
    static bool Prefix(VideoSpellPlayer __instance, ref Element __result)
    {
        try
        {
            if (__instance == null) return true;

            int slots = __instance.spellCounts?.Length ?? 5;
            int num;
            do
            {
                num = UnityEngine.Random.Range(0, slots);
            } while (__instance.spellCounts[num] == 0);
            __instance.activeSpellIndex = num;
            // call HighlightSpell
            var mi = typeof(VideoSpellPlayer).GetMethod("HighlightSpell", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(int) }, null);
            if (mi != null) mi.Invoke(__instance, new object[] { num });
            __result = VideoSpellPlayer.GetMappedElement(num);
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BalancePatch] ChooseRandomSpell patch error: {ex}");
            return true;
        }
    }
}

[HarmonyPatch(typeof(VideoSpellPlayer), "RandomCursor")]
public static class Patch_VideoSpellPlayer_RandomCursor_More
{
    static bool Prefix(VideoSpellPlayer __instance)
    {
        try
        {
            if (__instance == null) return true;

            int slots = __instance.spellCounts?.Length ?? 5;
            bool flag = false;
            bool flag2 = false;
            for (int i = 0; i < __instance.activeSpellIndex; i++)
            {
                if (i < __instance.spellCounts.Length && __instance.spellCounts[i] > 0)
                {
                    flag = true;
                    break;
                }
            }
            for (int j = __instance.activeSpellIndex + 1; j < slots; j++)
            {
                if (j < __instance.spellCounts.Length && __instance.spellCounts[j] > 0)
                {
                    flag2 = true;
                    break;
                }
            }
            if (!flag)
            {
                // IncrementCursor(false, true) => move down
                var mi = typeof(VideoSpellPlayer).GetMethod("IncrementCursor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                mi?.Invoke(__instance, new object[] { false, true });
                return false;
            }
            if (!flag2)
            {
                var mi2 = typeof(VideoSpellPlayer).GetMethod("IncrementCursor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                mi2?.Invoke(__instance, new object[] { true, true });
                return false;
            }
            var mi3 = typeof(VideoSpellPlayer).GetMethod("IncrementCursor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            bool dir = UnityEngine.Random.Range(0f, 1f) < 0.5f;
            mi3?.Invoke(__instance, new object[] { dir, true });
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BalancePatch] RandomCursor patch error: {ex}");
            return true;
        }
    }
}

[HarmonyPatch(typeof(VideoSpellPlayer), "IncrementCursor")]
public static class Patch_VideoSpellPlayer_IncrementCursor_More
{
    static bool Prefix(VideoSpellPlayer __instance, bool up, bool tellOtherClients = true)
    {
        try
        {
            if (__instance == null) return true;

            int slots = __instance.spellCounts?.Length ?? 5;
            __instance.activeSpellIndex =
                (__instance.activeSpellIndex + (up ? (slots - 1) : 1)) % slots;

            int guard = 0;
            while (guard < slots && __instance.spellCounts[__instance.activeSpellIndex] <= 0)
            {
                __instance.activeSpellIndex =
                    (__instance.activeSpellIndex + (up ? (slots - 1) : 1)) % slots;
                guard++;
            }

            if (Globals.online && tellOtherClients)
            {
                NetworkManager.current.SetDraftCursor(__instance.activeSpellIndex, false, 0);
            }

            if (CanHighlight(__instance))
            {
                Element e = VideoSpellPlayer.GetMappedElement(__instance.activeSpellIndex);
                int round = GameUtility.GetRound();
                __instance.HighlightSpell(e, round);
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BalancePatch] IncrementCursor patch error: {ex}");
            return true;
        }
    }
    static bool CanHighlight(VideoSpellPlayer vsp)
    {
        if (vsp == null) return false;
        if (vsp.playVideo == null) return false;

        var clipsField = typeof(PlayVideo).GetField("clips",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (clipsField == null) return false;

        var clips = clipsField.GetValue(vsp.playVideo) as VideoClip[];
        return clips != null && clips.Length > 0;
    }
}

