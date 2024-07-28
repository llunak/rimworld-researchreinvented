using System.Collections.Generic;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace PeteTimesSix.ResearchReinvented.Rimworld.WorkGivers
{

    public class WorkGiver_InquireForeigner : WorkGiver_Scanner
    {

	public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

	public override PathEndMode PathEndMode => PathEndMode.OnCell;

	public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
	{
		return pawn.Map.mapPawns.SlavesAndPrisonersOfColonySpawned;
	}

	public override bool ShouldSkip(Pawn pawn, bool forced = false)
	{
		return pawn.Map.mapPawns.SlavesAndPrisonersOfColonySpawnedCount == 0;
	}

        public static Type DriverClass = typeof(PeteTimesSix.ResearchReinvented.Rimworld.JobDrivers.JobDriver_InquireForeigner);

        private static ResearchProjectDef _matchingOpportunitiesCachedFor;
        private static ResearchOpportunity[] _matchingOpportunitesCache = Array.Empty<ResearchOpportunity>();
        public static IEnumerable<ResearchOpportunity> MatchingOpportunities
        {
            get
            {
                if (_matchingOpportunitiesCachedFor != Find.ResearchManager.GetProject())
                {
                    _matchingOpportunitesCache = ResearchOpportunityManager.Instance
                        .GetFilteredOpportunities(null, HandlingMode.Social).ToArray();
                        //.GetCurrentlyAvailableOpportunities(true)
                        //.Where(o => o.IsValid() && o.def.handledBy.HasFlag(HandlingMode.Social)).ToArray();
                    _matchingOpportunitiesCachedFor = Find.ResearchManager.GetProject();
                }
                return _matchingOpportunitesCache;
            }
        }
        public static void ClearMatchingOpportunityCache()
        {
            _matchingOpportunitiesCachedFor = null;
            _matchingOpportunitesCache = Array.Empty<ResearchOpportunity>();
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (base.ShouldSkip(pawn, forced))
                return true;

            if (Find.ResearchManager.GetProject() == null)
                return true;

            return !MatchingOpportunities.Any(o => !o.IsFinished);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            Pawn otherPawn = (Pawn)thing; 

            if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Research))
            {
                JobFailReason.Is(StringsCache.JobFail_IncapableOfResearch, null);
                return false;
            }
            if (!pawn.workSettings?.WorkIsActive(WorkTypeDefOf.Research) ?? true)
            {
                JobFailReason.Is("NotAssignedToWorkType".Translate(WorkTypeDefOf.Research.gerundLabel).CapitalizeFirst(), null);
                return false;
            }
            if ((!otherPawn.guest.ScheduledForInteraction))
            {
                JobFailReason.Is("PrisonerInteractedTooRecently".Translate(), null);
                return false;
            }
            if (!MatchingOpportunities.Any(o => o.CurrentAvailability == OpportunityAvailability.Available && o.requirement.MetBy(otherPawn)))
            {
                JobFailReason.Is("RR_jobFail_PrisonerHasNothingToTeach".Translate(), null);
                return false;
            }

            return base.HasJobOnThing(pawn, thing, forced);
        }

        public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
        {
            Pawn otherPawn = (Pawn)thing;

            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking) || (otherPawn.Downed && !otherPawn.InBed()) || !pawn.CanReserve(otherPawn, 1, -1, null, false) || !otherPawn.Awake())
            {
                return null;
            }
            return JobMaker.MakeJob(JobDefOf_Custom.RR_InquireForeigner, otherPawn);
        }
    }
}
