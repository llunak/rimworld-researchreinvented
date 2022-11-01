﻿using PeteTimesSix.ResearchReinvented.Defs;
using PeteTimesSix.ResearchReinvented.Opportunities;
using PeteTimesSix.ResearchReinvented.OpportunityComps;
using PeteTimesSix.ResearchReinvented.Utilities;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace PeteTimesSix.ResearchReinvented.Managers
{
    public class ResearchOpportunityManager : GameComponent
    {
        public static ResearchOpportunityManager instance => Current.Game.GetComponent<ResearchOpportunityManager>();


        private List<ResearchOpportunity> _allGeneratedOpportunities = new List<ResearchOpportunity>();

        public IReadOnlyCollection<ResearchOpportunity> AllGeneratedOpportunities => _allGeneratedOpportunities.AsReadOnly();

        private ResearchProjectDef _currentProject;
        public ResearchProjectDef CurrentProject => _currentProject;
        private List<ResearchOpportunity> _currentProjectOpportunitiesCache;
        public IReadOnlyCollection<ResearchOpportunity> CurrentProjectOpportunities
        {
            get
            {
                bool currentProjectRegenned = CheckForRegeneration();
                if(currentProjectRegenned || _currentProjectOpportunitiesCache == null) 
                {
                    _currentProjectOpportunitiesCache = _allGeneratedOpportunities.Where(o => o.project == _currentProject).ToList();
                }
                return _currentProjectOpportunitiesCache.AsReadOnly();
            }
        }
        private HashSet<ResearchOpportunityCategoryDef> _currentOpportunityCategoriesCache;
        public IReadOnlyCollection<ResearchOpportunityCategoryDef> CurrentProjectOpportunityCategories
        {
            get
            {
                bool currentProjectRegenned = CheckForRegeneration();
                if (currentProjectRegenned || _currentOpportunityCategoriesCache == null)
                {
                    _currentOpportunityCategoriesCache = CurrentProjectOpportunities.Select(o => o.def.GetCategory(o.relation)).ToHashSet();
                }
                return _currentOpportunityCategoriesCache.ToList().AsReadOnly();
            }
        }
        public HashSet<ResearchProjectDef> _projectsGenerated = new HashSet<ResearchProjectDef>();

        private Dictionary<int, ResearchOpportunity> _jobToOpportunityMap = new Dictionary<int, ResearchOpportunity>();
        private List<ResearchOpportunityCategoryTotalsStore> _categoryStores = new List<ResearchOpportunityCategoryTotalsStore>();
       
        
        // contributed by mahenry00
        public override void LoadedGame()
        {
            base.LoadedGame();
            _allGeneratedOpportunities = _allGeneratedOpportunities
            .Where(o =>
            {
                if (!o.IsValid())
                {
                    Log.Warning($"[RR]: Research opportunity invalid, loadID: {o.loadID}, project: {o.project.label}");
                    return false;
                }
                return true;
            })
            .ToList();

            DoBackwardsCompatChecks();

            CheckForRegeneration();
        }

        private void DoBackwardsCompatChecks()
        {
            foreach (var op in _allGeneratedOpportunities) 
            {
                if(op.requirement is ROComp_RequiresThing requiresThing) 
                {
                    if(requiresThing.thingDef.IsCorpse)
                    {
                        var innerDef = requiresThing.thingDef?.ingestible?.sourceDef;
                        if (innerDef != null)
                            requiresThing.thingDef = innerDef;
                    }
                }
            }
        }

        public bool CheckForRegeneration() 
        {
            if(Find.ResearchManager.currentProj != _currentProject)
            {
                GenerateOpportunities(Find.ResearchManager.currentProj, false);
                return true;
            }
            return false;
        }

        public IEnumerable<ResearchOpportunity> GetCurrentlyAvailableOpportunities(bool includeFinished = false)
        {
            bool currentProjectRegenned = CheckForRegeneration();
            if (!includeFinished)
                return CurrentProjectOpportunities.Where(o => o.IsValid() && o.CurrentAvailability == OpportunityAvailability.Available);
            else
                return CurrentProjectOpportunities.Where(o => o.IsValid() && (o.CurrentAvailability == OpportunityAvailability.Available || o.CurrentAvailability == OpportunityAvailability.Finished));

        }

        internal ResearchOpportunityCategoryTotalsStore GetTotalsStore(ResearchProjectDef project, ResearchOpportunityCategoryDef category)
        {
            return _categoryStores.FirstOrDefault(cs => cs.project == project && cs.category == category);
        }

        public ResearchOpportunityManager(Game game)
        {
        }

        private List<int> wList1;
        private List<ResearchOpportunity> wList2;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _allGeneratedOpportunities, "_allGeneratedOpportunities", LookMode.Deep);
            Scribe_Collections.Look(ref _projectsGenerated, "_allProjectsWithGeneratedOpportunities", LookMode.Def);
            Scribe_Collections.Look(ref _jobToOpportunityMap, "_jobToOpportunityMap", LookMode.Value, LookMode.Reference, ref wList1, ref wList2);
            Scribe_Defs.Look(ref _currentProject, "currentProject");
            Scribe_Collections.Look(ref _categoryStores, "_categoryStores", LookMode.Deep);
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (_jobToOpportunityMap == null)
                _jobToOpportunityMap = new Dictionary<int, ResearchOpportunity>();
            if (_categoryStores == null)
                _categoryStores = new List<ResearchOpportunityCategoryTotalsStore>();
        }

        public void FinishProject(ResearchProjectDef project, bool doCompletionDialog = false, Pawn researcher = null)
        {
            Find.ResearchManager.FinishProject(project, doCompletionDialog, researcher);
        }
        
        public void GenerateOpportunities(ResearchProjectDef project, bool forceRegen)
        {
            if(_currentProject == project && !forceRegen) 
            {
                return;
            }
            _currentProjectOpportunitiesCache = null;
            _currentOpportunityCategoriesCache = null;
            _currentProject = project;
            if (project == null)
                return;

            if (_projectsGenerated.Contains(project)) 
            {
                if (forceRegen)
                {
                    var preexistingOpportunities = _allGeneratedOpportunities.Where(o => o.project == project).ToList();
                    foreach (var preexisting in preexistingOpportunities)
                        _allGeneratedOpportunities.Remove(preexisting);
                }
                else
                {
                    return;
                }
            }

            var results = ResearchOpportunityPrefabs.MakeOpportunitiesForProject(project);
            var newOpportunities = results.opportunities;
            var categoryStores = results.categoryStores;
            _categoryStores.RemoveAll(cs => cs.project == project);
            _categoryStores.AddRange(categoryStores);

            var invalidOpportunities = newOpportunities.Where(o => !o.IsValid());
            if (invalidOpportunities.Any())
                Log.Warning($"Generated {invalidOpportunities.Count()} invalid opportunities for project {project}!");

            _allGeneratedOpportunities.AddRange(newOpportunities.Where(o => o.IsValid()));
            _projectsGenerated.Add(project);

            if (ResearchReinventedMod.Settings.debugPrintouts)
            {
                Debug.LogMessage($"Listing generated opportunities for current project {_currentProject.label}...");
                foreach (var opportunity in newOpportunities)
                {
                    Debug.LogMessage($" |-- {opportunity.ShortDesc}");
                }
            }
        }

        public void AssociateJobWithOpportunity(Pawn pawn, Job job, ResearchOpportunity opportunity)
        {
            if (opportunity != null)
                _jobToOpportunityMap[job.loadID] = opportunity;
            else
                Log.Warning($"attempted to associate job {job} for {pawn} with null opportunity");
            //Log.Message($"pawn {pawn} associated job {job} (load id: {job.GetUniqueLoadID()}) with opportunity {opportunity} (load id: {opportunity.GetUniqueLoadID()})");
        }

        public void ClearAssociatedJobWithOpportunity(Pawn pawn, Job job)
        {
            if (_jobToOpportunityMap.ContainsKey(job.loadID))
                _jobToOpportunityMap.Remove(job.loadID);
            //Log.Message($"pawn {pawn} cleared associated job {job} (load id: {job.GetUniqueLoadID()})");
        }

        public ResearchOpportunity GetOpportunityForJob(Job job)
        {
            if (_jobToOpportunityMap.TryGetValue(job.loadID, out ResearchOpportunity result))
                return result;
            else
                return null;
        }
    }
}
