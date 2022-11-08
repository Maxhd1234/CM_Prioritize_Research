using HarmonyLib;
using RimWorld;
using Verse;

namespace KB_Prioritize_Research
{
    public class PrioritizeResearchMod : Mod
    {
        private static PrioritizeResearchMod _instance;
        public static PrioritizeResearchMod Instance => _instance;

        public PrioritizeResearchMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("KB_Prioritize_Research");
            harmony.PatchAll();

            _instance = this;
        }
    }
}
