﻿using Binarysharp.MemoryManagement;
using Binarysharp.MemoryManagement.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anathema
{
    class FilterFSM : IFilterFSMModel
    {
        // Snapshot being labeled with change counts
        private Snapshot Snapshot;

        // User controlled variables
        private ScanConstraints ScanConstraints;

        public FilterFSM()
        {
            ScanConstraints = new ScanConstraints();
        }

        public override void SetElementType(Type ElementType)
        {
            ScanConstraints.SetElementType(ElementType);
        }

        public override Type GetElementType()
        {
            return ScanConstraints.GetElementType();
        }

        public override void AddConstraint(ValueConstraintsEnum ValueConstraint, dynamic Value)
        {
            ScanConstraints.AddConstraint(new ScanConstraintItem(ValueConstraint, Value));
            UpdateDisplay();
        }

        public override void RemoveConstraints(Int32[] ConstraintIndicies)
        {
            ScanConstraints.RemoveConstraints(ConstraintIndicies);
            UpdateDisplay();
        }

        public override void ClearConstraints()
        {
            ScanConstraints.ClearConstraints();
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            FilterFSMEventArgs FilterFSMEventArgs = new FilterFSMEventArgs();
            FilterFSMEventArgs.ScanConstraints = ScanConstraints;
            OnEventUpdateDisplay(FilterFSMEventArgs);
        }

        public override void BeginScan()
        {
            // Initialize snapshot
            Snapshot = new Snapshot(SnapshotManager.GetInstance().GetActiveSnapshot());
            Snapshot.MarkAllValid();
            Snapshot.SetElementType(ScanConstraints.GetElementType());

            base.BeginScanRunOnce();
        }

        protected override void UpdateScan()
        {
            // Read memory to get current values
            Snapshot.ReadAllSnapshotMemory();

            // Enforce each value constraint
            foreach (ScanConstraintItem ScanConstraint in ScanConstraints)
            {

                Parallel.ForEach(Snapshot.Cast<Object>(), (RegionObject) =>
                {
                    SnapshotRegion Region = (SnapshotRegion)RegionObject;

                    if (!Region.CanCompare())
                        return;

                    foreach (SnapshotElement Element in Region)
                    {
                        if (!Element.Valid)
                            continue;

                        switch (ScanConstraint.ValueConstraints)
                        {
                            case ValueConstraintsEnum.Unchanged:
                                if (!Element.Unchanged())
                                    Element.Valid = false;
                                break;
                            case ValueConstraintsEnum.Changed:
                                if (!Element.Changed())
                                    Element.Valid = false;
                                break;
                            case ValueConstraintsEnum.Increased:
                                if (!Element.Increased())
                                    Element.Valid = false;
                                break;
                            case ValueConstraintsEnum.Decreased:
                                if (!Element.Decreased())
                                    Element.Valid = false;
                                break;
                            case ValueConstraintsEnum.IncreasedByX:
                                if (!Element.IncreasedByValue(ScanConstraint.Value))
                                    Element.Valid = false;
                                break;
                            case ValueConstraintsEnum.DecreasedByX:
                                if (!Element.DecreasedByValue(ScanConstraint.Value))
                                    Element.Valid = false;
                                break;
                            case ValueConstraintsEnum.Equal:
                                if (!Element.EqualToValue(ScanConstraint.Value))
                                    Element.Valid = false;
                                break;
                            case ValueConstraintsEnum.NotEqual:
                                if (!Element.NotEqualToValue(ScanConstraint.Value))
                                    Element.Valid = false;
                                break;
                            case ValueConstraintsEnum.GreaterThan:
                                if (!Element.GreaterThanValue(ScanConstraint.Value))
                                    Element.Valid = false;
                                break;
                            case ValueConstraintsEnum.LessThan:
                                if (!Element.LessThanValue(ScanConstraint.Value))
                                    Element.Valid = false;
                                break;
                        }

                    } // End foreach Element

                }); // End foreach Region

            } // End foreach Constraint
        }

        public override void EndScan()
        {
            // base.EndScan();
            Snapshot.ExpandValidRegions();
            Snapshot FilteredSnapshot = new Snapshot(Snapshot.GetValidRegions());
            FilteredSnapshot.SetScanMethod("Manual Scan");

            SnapshotManager.GetInstance().SaveSnapshot(FilteredSnapshot);
        }
    }
}