﻿using PeteTimesSix.ResearchReinvented.Data;
using PeteTimesSix.ResearchReinvented.Defs;
using PeteTimesSix.ResearchReinvented.Managers;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PeteTimesSix.ResearchReinvented.Rimworld.InteractionWorkers
{
    public class InteractionWorker_LearnScienceFromPrisoner : InteractionWorker
    {
        public float BaseResearchAmount => 10f;

        public SimpleCurve moodCurve = new SimpleCurve()
        {
            Points = {
                new CurvePoint(0f, 0),
                new CurvePoint(0.1f, 0),
                new CurvePoint(0.75f, 1f),
                new CurvePoint(1f, 1f)
            }
        };

        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets)
        {
            letterText = null;
            letterLabel = null;
            letterDef = null;
            lookTargets = null;

            var opportunity = ResearchOpportunityManager.Instance.GetCurrentlyAvailableOpportunities()
                   .Where(o => o.def.handledBy.HasFlag(HandlingMode.Social) && o.requirement.MetBy(recipient))
            .FirstOrDefault();

            if (opportunity != null)
            {
                var amount = BaseResearchAmounts.InteractionLearnFromPrisoner;
                var modifier = initiator.GetStatValue(StatDefOf.NegotiationAbility) * Math.Max(initiator.GetStatValue(StatDefOf.ResearchSpeed), recipient.GetStatValue(StatDefOf.ResearchSpeed));

                if(recipient.needs?.mood != null) 
                {
                    var moodPercent = recipient.needs.mood.CurLevelPercentage;
                    var moodModifier = moodCurve.Evaluate(moodPercent);
                    modifier *= moodModifier;
                }
                opportunity.ResearchChunkPerformed(initiator, HandlingMode.Social, amount, modifier, SkillDefOf.Intellectual, recipient.Faction?.Name);
            }
        }
    }
}
