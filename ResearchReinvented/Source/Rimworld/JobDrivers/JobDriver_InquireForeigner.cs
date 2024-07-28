using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;

namespace PeteTimesSix.ResearchReinvented.Rimworld.JobDrivers
{
    public class JobDriver_InquireForeigner : JobDriver
    {
        public static readonly int REPETITIONS = 4;
        protected Pawn Talkee => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Talkee, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnMentalState(TargetIndex.A);
            this.FailOnAggroMentalStateAndHostile(TargetIndex.A);
            this.FailOnNotAwake(TargetIndex.A);

            yield return Toils_Interpersonal.GotoInteractablePosition(TargetIndex.A);
            yield return Toils_Interpersonal.WaitToBeAbleToInteract(Talkee);
            for (int i = 0; i < REPETITIONS; i++)
            {
                yield return Toils_Interpersonal.GotoInteractablePosition(TargetIndex.A);
                yield return Toils_Interpersonal.WaitToBeAbleToInteract(this.Talkee);
                yield return ScienceInterrogationRequest(this.pawn, this.Talkee);
                yield return Toils_Interpersonal.GotoInteractablePosition(TargetIndex.A);
                yield return Toils_Interpersonal.WaitToBeAbleToInteract(this.Talkee);
                yield return ScienceInterrogationReply(this.pawn, this.Talkee);
            }
            yield return Toils_Interpersonal.GotoInteractablePosition(TargetIndex.A);
            yield return Toils_Interpersonal.WaitToBeAbleToInteract(Talkee);
            yield return Toils_Interpersonal.SetLastInteractTime(TargetIndex.A);
            yield return ScienceInterrogationFinalize(this.pawn, this.Talkee);
            yield break;
        }
    }
}
