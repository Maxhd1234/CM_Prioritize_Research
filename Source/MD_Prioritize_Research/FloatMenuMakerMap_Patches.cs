using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static UnityEngine.GraphicsBuffer;
using static Global;


public static class Global
{


    public static bool stopRn;
    public class ResearchJobEnforcer : MapComponent
    {
        public Pawn pawn;
        public Building_ResearchBench bench;

        public ResearchJobEnforcer(Map map) : base(map) { }

        public void SetTarget(Pawn p, Building_ResearchBench b)
        {
            pawn = p;
            bench = b;
        }

        public override void MapComponentTick()
        {
            if (stopRn)
            {
                pawn = null;
                bench = null;
                stopRn = false;
                return;
            }

            if (pawn == null || bench == null || pawn.Dead || !pawn.Spawned)
                return;

            if (pawn.CurJobDef != JobDefOf.Research)
            {
                var job = JobMaker.MakeJob(JobDefOf.Research, bench);
                job.playerForced = true;
                job.checkOverrideOnExpire = true;
                job.playerInterruptedForced = true;
                job.count = 5134;
                pawn.jobs.ClearQueuedJobs();
                pawn.jobs.TryTakeOrderedJob(job);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
    public static class Patch_JobTracker_StartJob
    {
        public static void Prefix(Pawn_JobTracker __instance, Job newJob)
        {
            if (newJob.playerForced && newJob.count != 5134)
            {
                stopRn = true;
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
                        stopRn = true;

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
                            enforcer.pawn = pawn;
                            enforcer.bench = bench;
                        }
                    }
                });

                return false; 
            }

            return true;
        }

    }
    
}



