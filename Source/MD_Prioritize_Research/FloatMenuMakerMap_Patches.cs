using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System;
using System.Collections.Generic;
using System.Linq;


public static class Global
{

    public class ResearchJobEnforcer : MapComponent
    {
        public Pawn pawn;
        public Building_ResearchBench bench;
        public bool stopResearch = true;

        public ResearchJobEnforcer(Map map) : base(map) { }

        public void StopResearch()
        {
            stopResearch = true;
            if (pawn != null)
            {
                pawn.jobs.ClearQueuedJobs();
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
            pawn = null;
            bench = null;
        }

        public void SetTarget(Pawn p, Building_ResearchBench b)
        {
            if (p != null && b != null && p.Map == map && b.Map == map)
            {
                pawn = p;
                bench = b;
                stopResearch = false;
            }
        }

        public override void MapComponentTick()
        {
            if (!stopResearch)
            {
                if (pawn == null || bench == null || pawn.Dead || !pawn.Spawned || pawn.Map != map || bench.Map != map || bench.Destroyed)
                {
                    Clear();
                    return;
                }

                if (pawn.jobs == null || pawn.CurJobDef == null)
                {
                    Clear();
                    return;
                }

                if (pawn.CurJobDef != JobDefOf.Research)
                {
                    var job = JobMaker.MakeJob(JobDefOf.Research, bench);
                    job.playerForced = true;
                    job.count = 5134;
                    pawn.jobs.ClearQueuedJobs();
                    pawn.jobs.TryTakeOrderedJob(job);
                }
            }
        }

        private void Clear()
        {
            pawn = null;
            bench = null;
        }
    }


    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    public static class Patch_JobTracker_StartJob
    {
        public static void Prefix(Pawn_JobTracker __instance, Job newJob)
        {
            if (newJob.playerForced && newJob.count != 5134)
            {
                var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                if (pawn != null)
                {
                    var enforcer = pawn.Map.GetComponent<ResearchJobEnforcer>();
                    if (enforcer != null && !enforcer.stopResearch)
                    {
                        enforcer.StopResearch();
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PriorityWork), nameof(PriorityWork.GetGizmos))]
    public static class Patch_PriorityWork_GetGizmos
    {
        public static void Postfix(PriorityWork __instance, ref IEnumerable<Gizmo> __result)
        {

            var list = __result.ToList();

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is Command_Action ca &&
                    ca.defaultLabel == "CommandClearPrioritizedWork".Translate())
                {
                    var originalAction = ca.action;

                    ca.action = () =>
                    {
                        var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                        if (pawn != null)
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
                            var enforcer = pawn.Map.GetComponent<ResearchJobEnforcer>();
                            if (enforcer != null)
                            {
                                if (enforcer.stopResearch == false)
                                    enforcer.StopResearch();
                            }
                            else
                            {
                                Log.Error("Enforcer is Null");
                            }
                        }
                        __instance.ClearPrioritizedWorkAndJobQueue();

                        originalAction?.Invoke();
                    };

                    list[i] = ca;
                    break;
                }
            }

            __result = list;
        }
    }





    [HarmonyPatch(typeof(FloatMenuOptionProvider_WorkGivers), "GetWorkGiverOption", new Type[] {
    typeof(Pawn), typeof(WorkGiverDef), typeof(LocalTargetInfo), typeof(FloatMenuContext)
})]
    public static class Patch_Research_GetSingleOption_Prefix
    {

        public static bool Prefix(
    ref FloatMenuOption __result,
    Pawn pawn,
    WorkGiverDef workGiver,
    LocalTargetInfo target,
    FloatMenuContext context)
        {
            if (pawn == null || workGiver == null || target.Thing == null)
                return true;

            var field = AccessTools.Field(typeof(ResearchManager), "currentProj");
            ResearchProjectDef current = (ResearchProjectDef)field.GetValue(Find.ResearchManager);

            if (current == null)
                return true;

            if (workGiver.defName != "Research")
                return true;

            if (target.Thing is not Building_ResearchBench bench)
                return true;

            if (!pawn.WorkTagIsDisabled(WorkTags.Intellectual) &&
                pawn.workSettings.WorkIsActive(WorkTypeDefOf.Research))
            {
                __result = new FloatMenuOption("PrioritizeGeneric".Translate(workGiver.Worker.def.label, "").CapitalizeFirst(), () =>
                {
                    Job job = JobMaker.MakeJob(JobDefOf.Research, bench);
                    if (job != null)
                    {
                        job.playerForced = true;
                        job.count = 5134;
                        pawn.jobs.ClearQueuedJobs();
                        pawn.jobs.TryTakeOrderedJobPrioritizedWork(job, workGiver.Worker, context.ClickedCell);

                        var enforcer = pawn.Map.GetComponent<ResearchJobEnforcer>();
                        if (enforcer != null)
                        {
                            enforcer.SetTarget(pawn, bench);
                        }
                        else
                        {
                            Log.Error("Enforcer is Null");
                        }
                    }
                });

                return false; 
            }

            return true;
        }

    }
    
}



