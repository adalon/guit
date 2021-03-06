﻿using System.Linq;
using System.Composition;
using LibGit2Sharp;
using Terminal.Gui;
using System.Collections.Generic;
using Guit.Plugin.Sync.Properties;

namespace Guit.Plugin.Sync
{
    [Shared]
    [Export]
    [ContentView(WellKnownViews.Sync, '2', resources: typeof(Resources))]
    public class SyncView : ContentView
    {
        readonly IRepository repository;
        readonly IHistoryDivergenceService historyDivergenceService;

        readonly ListView<Commit> aheadListView;
        readonly ListView<Commit> behindListView;
        readonly FrameView aheadFrameView;
        readonly FrameView behindFrameView;

        readonly ColumnDefinition<Commit>[] columnDefinitions = new[]
        {
                new ColumnDefinition<Commit>(x => x.GetShortSha(), 10),
                new ColumnDefinition<Commit>(x => x.MessageShort, "*")
        };

        [ImportingConstructor]
        public SyncView(IRepository repository, IHistoryDivergenceService historyDivergenceService)
            : base(Resources.Sync)
        {
            this.repository = repository;
            this.historyDivergenceService = historyDivergenceService;

            aheadListView = new ListView<Commit>(columnDefinitions);
            aheadFrameView = new FrameView(string.Empty) { Y = 1, Width = Dim.Percent(50) };
            aheadFrameView.Add(aheadListView);

            behindListView = new ListView<Commit>(columnDefinitions);
            behindFrameView = new FrameView(string.Empty) { Y = 1, Width = Dim.Percent(100) };
            behindFrameView.Add(behindListView);

            Content = new StackPanel(StackPanelOrientation.Horizontal, aheadFrameView, behindFrameView);
        }

        public override void Refresh()
        {
            base.Refresh();

            var sourceBranch = repository.Head;
            var targetBranch = GetCandidateTargetBranches(sourceBranch)
                .FirstOrDefault(target => HasDivergence(sourceBranch, target));

            if (targetBranch != null && historyDivergenceService.TryGetDivergence(repository, sourceBranch, targetBranch, out var aheadCommits))
            {
                aheadListView.SetValues(aheadCommits.ToList());
                aheadFrameView.Title = string.Format("{0} commits ahead {1}", aheadListView.Values.Count(), targetBranch.FriendlyName);
            }
            else
            {
                aheadListView.SetValues(Enumerable.Empty<Commit>());
                aheadFrameView.Title = "Up to date!";
            }

            if (targetBranch != null && historyDivergenceService.TryGetDivergence(repository, targetBranch, sourceBranch, out var behindCommits))
            {
                behindListView.SetValues(behindCommits.ToList());
                behindFrameView.Title = string.Format("{0} commits behind {1}", behindListView.Values.Count(), targetBranch.FriendlyName);
            }
            else
            {
                behindListView.SetValues(Enumerable.Empty<Commit>());
                behindFrameView.Title = "Up to date!";
            }
        }

        bool HasDivergence(Branch source, Branch? target) =>
            target != null &&
            (historyDivergenceService.HasDivergence(repository, source, target) ||
             historyDivergenceService.HasDivergence(repository, target, source));

        IEnumerable<Branch> GetCandidateTargetBranches(Branch branch)
        {
            yield return branch.TrackedBranch;
            yield return repository.Branches.FirstOrDefault(x => x.FriendlyName == "origin/" + branch.FriendlyName);
            yield return repository.Branches.FirstOrDefault(x => x.FriendlyName == "origin/master");
        }
    }
}