using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using System;
using Verse.AI;
using Verse;

[HarmonyPatch(typeof(FloatMenuOptionProvider_WorkGivers), "GetSingleOptionFor")]
public static class GetWorkGiverOption_Transpiler_Patch_Final
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        bool patched = false;


        for (int i = 0; i < codes.Count - 2; i++)
        {
            // Pattern: ldfld Verse.JobDef def -> ldsfld Verse.JobDef Research -> bne.un.s
            if (codes[i].LoadsField(AccessTools.Field(typeof(Job), "def")) &&
                codes[i + 1].LoadsField(AccessTools.Field(typeof(JobDefOf), "Research")) &&
                codes[i + 2].opcode == OpCodes.Bne_Un_S)
            {
                Log.Message($"[KB_Prioritize_Research] Found research check pattern at index {i}, patching...");

                // Replace the JobDefOf.Research with null to make the comparison always false
                // This effectively removes the research restriction
                codes[i + 1] = new CodeInstruction(OpCodes.Ldnull);

                patched = true;
                Log.Message("[KB_Prioritize_Research] Successfully patched research restriction!");
                break;
            }
        }

        if (!patched)
        {
            Log.Error("[KB_Prioritize_Research] Failed to find research check pattern!");
        }

        return codes;
    }
}