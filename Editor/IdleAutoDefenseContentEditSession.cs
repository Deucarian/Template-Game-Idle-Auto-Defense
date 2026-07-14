using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Deucarian.GameContentAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    [InitializeOnLoad]
    internal static class IdleAutoDefenseUnityUndoBridge
    {
        private static readonly List<WeakReference<GameContentPackAuthoringProvider>> Providers =
            new List<WeakReference<GameContentPackAuthoringProvider>>();
        private static readonly List<WeakReference<IdleAutoDefenseContentEditSession>> Sessions =
            new List<WeakReference<IdleAutoDefenseContentEditSession>>();

        static IdleAutoDefenseUnityUndoBridge()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;
            AssemblyReloadEvents.beforeAssemblyReload -= Shutdown;
            AssemblyReloadEvents.beforeAssemblyReload += Shutdown;
        }

        public static void RegisterProvider(GameContentPackAuthoringProvider provider)
        {
            if (provider == null) return;
            RemoveDead(Providers);
            if (Providers.Any(reference => reference.TryGetTarget(out GameContentPackAuthoringProvider current) &&
                                           ReferenceEquals(current, provider)))
                return;
            Providers.Add(new WeakReference<GameContentPackAuthoringProvider>(provider));
        }

        public static void RegisterSession(IdleAutoDefenseContentEditSession session)
        {
            if (session == null) return;
            RemoveDead(Sessions);
            Sessions.Add(new WeakReference<IdleAutoDefenseContentEditSession>(session));
        }

        public static void UnregisterSession(IdleAutoDefenseContentEditSession session)
        {
            for (int i = Sessions.Count - 1; i >= 0; i--)
            {
                if (!Sessions[i].TryGetTarget(out IdleAutoDefenseContentEditSession current) ||
                    ReferenceEquals(current, session))
                    Sessions.RemoveAt(i);
            }
        }

        private static void OnUndoRedo()
        {
            for (int i = Providers.Count - 1; i >= 0; i--)
            {
                if (!Providers[i].TryGetTarget(out GameContentPackAuthoringProvider provider))
                {
                    Providers.RemoveAt(i);
                    continue;
                }

                try
                {
                    provider.RefreshAfterExternalEdit();
                }
                catch
                {
                    // The next explicit provider refresh will surface any source problem.
                }
            }

            for (int i = Sessions.Count - 1; i >= 0; i--)
            {
                if (!Sessions[i].TryGetTarget(out IdleAutoDefenseContentEditSession session))
                {
                    Sessions.RemoveAt(i);
                    continue;
                }

                session.OnUnityUndoRedo();
            }

            GameContentAuthoringWindow[] windows = Resources.FindObjectsOfTypeAll<GameContentAuthoringWindow>();
            for (int i = 0; i < windows.Length; i++) windows[i].Repaint();
        }

        private static void Shutdown()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            AssemblyReloadEvents.beforeAssemblyReload -= Shutdown;
            Providers.Clear();
            Sessions.Clear();
        }

        private static void RemoveDead<T>(IList<WeakReference<T>> references) where T : class
        {
            for (int i = references.Count - 1; i >= 0; i--)
                if (!references[i].TryGetTarget(out _)) references.RemoveAt(i);
        }
    }

    internal sealed class IdleAutoDefenseContentEditSession :
        IGameContentEditSession,
        IGameContentRecordReferenceEditSession,
        IGameContentOrderedCollectionEditSession
    {
        private readonly GameContentPackAuthoringProvider _provider;
        private readonly GameContentEditRequest _request;
        private readonly GameContentSourceRevision _originalRevision;
        private readonly IReadOnlyDictionary<string, GameContentFieldValue> _originalValues;
        private readonly byte[] _originalFileBytes;
        private readonly IReadOnlyList<GameContentFieldDescriptor> _fields;
        private readonly List<Dictionary<string, GameContentFieldValue>> _history =
            new List<Dictionary<string, GameContentFieldValue>>();
        private IdleAutoDefenseEditableSource _source;
        private GameContentSourceRevision _committedRevision;
        private byte[] _committedFileBytes;
        private GameContentEditSessionState _state;
        private int _historyIndex;
        private bool _disposed;

        public IdleAutoDefenseContentEditSession(
            GameContentPackAuthoringProvider provider,
            GameContentEditRequest request,
            IdleAutoDefenseEditableSource source,
            GameContentSourceRevision originalRevision,
            IReadOnlyDictionary<string, GameContentFieldValue> originalValues)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _originalRevision = originalRevision ?? throw new ArgumentNullException(nameof(originalRevision));
            _originalValues = CopyValues(originalValues);
            _originalFileBytes = ReadSourceBytes(source.SourcePath);
            GameContentSourceRevision confirmedRevision = IdleAutoDefenseSourceRevision.Create(request, source);
            if (!confirmedRevision.Equals(originalRevision))
                throw new InvalidOperationException("The editable source changed while the edit snapshot was being captured. Reopen the record and try again.");
            _fields = source.Mappings.Select(value => value.Descriptor).ToArray();
            _history.Add(CopyValues(originalValues));
            Snapshot = new GameContentEditSnapshot(
                request.RecordKey,
                source.SourceTarget,
                originalRevision,
                _originalValues,
                DateTime.UtcNow,
                IdleAutoDefenseSourceRevision.SchemaToken);
            _state = GameContentEditSessionState.Clean;
            IdleAutoDefenseUnityUndoBridge.RegisterSession(this);
        }

        public string BackendId => GameContentPackAuthoringProvider.ContentPackProviderId;
        public GameContentRecordKey RecordKey => _request.RecordKey;
        public GameContentSourceTarget SourceTarget => _source.SourceTarget;
        public GameContentSourceRevision OriginalRevision => _originalRevision;
        public GameContentEditSessionState State => _state;
        public GameContentEditSnapshot Snapshot { get; }
        public IReadOnlyList<GameContentFieldDescriptor> Fields => _fields;
        public IReadOnlyList<GameContentProposedChange> Changes => BuildChanges(CurrentValues);
        public bool CanUndo => IsMutable && _historyIndex > 0;
        public bool CanRedo => IsMutable && _historyIndex < _history.Count - 1;

        private bool IsMutable => !_disposed &&
                                  (_state == GameContentEditSessionState.Clean ||
                                   _state == GameContentEditSessionState.Dirty);
        private IReadOnlyDictionary<string, GameContentFieldValue> CurrentValues => _history[_historyIndex];

        public GameContentEditOperationResult Apply(string fieldId, GameContentFieldValue value)
        {
            if (!IsMutable) return GameContentEditOperationResult.Failure("The edit session cannot accept staged changes in its current state.");
            GameContentStaleCheckResult stale = CheckStale();
            if (stale.IsStale) return GameContentEditOperationResult.Failure(stale.Message);
            IdleAutoDefenseSerializedFieldMapping mapping = _source.Mappings.FirstOrDefault(candidate =>
                string.Equals(candidate.Descriptor.FieldId, fieldId, StringComparison.Ordinal));
            if (mapping == null) return GameContentEditOperationResult.Failure("The field is not part of the provider-owned edit whitelist.");
            if (mapping.Descriptor.FieldType.IsOrderedCollection())
                return GameContentEditOperationResult.Failure("Use an ordered collection operation to edit this field.");
            if (!mapping.Descriptor.Accepts(value, out string reason))
                return GameContentEditOperationResult.Failure(reason);
            if (mapping.Descriptor.FieldType == GameContentFieldType.RecordReference)
            {
                GameContentReferenceEvaluation evaluation = EvaluateReferenceTargetCore(
                    mapping.Descriptor.FieldId,
                    value.RecordReferenceValue?.TargetKey);
                if (!evaluation.IsValid) return GameContentEditOperationResult.Failure(evaluation.Reason);
            }
            if (CurrentValues.TryGetValue(mapping.Descriptor.FieldId, out GameContentFieldValue current) && current.Equals(value))
                return GameContentEditOperationResult.Success("The staged value is already current.");

            return StageValue(mapping, value, "Staged " + mapping.Descriptor.DisplayName + ".");
        }

        public GameContentEditOperationResult ApplyCollectionOperation(
            string fieldId,
            GameContentCollectionOperation operation)
        {
            if (!IsMutable) return GameContentEditOperationResult.Failure("The edit session cannot accept staged changes in its current state.");
            GameContentStaleCheckResult stale = CheckStale();
            if (stale.IsStale) return GameContentEditOperationResult.Failure(stale.Message);
            IdleAutoDefenseSerializedFieldMapping mapping = _source.Mappings.FirstOrDefault(candidate =>
                string.Equals(candidate.Descriptor.FieldId, fieldId, StringComparison.Ordinal));
            if (mapping == null ||
                mapping.Descriptor.FieldType != GameContentFieldType.OrderedRecordReferenceCollection ||
                mapping.Descriptor.Collection == null)
                return GameContentEditOperationResult.Failure("The field is not the approved run-profile Waves collection.");
            if (!CurrentValues.TryGetValue(mapping.Descriptor.FieldId, out GameContentFieldValue currentValue) ||
                currentValue.OrderedCollectionValue == null)
                return GameContentEditOperationResult.Failure("The staged Waves collection is unavailable.");

            if (operation != null &&
                (operation.Kind == GameContentCollectionOperationKind.Add ||
                 operation.Kind == GameContentCollectionOperationKind.Replace))
            {
                GameContentRecordReferenceValue reference = operation.Value?.RecordReferenceValue;
                GameContentReferenceEvaluation evaluation = EvaluateReferenceTargetCore(
                    mapping.Descriptor.FieldId,
                    reference?.TargetKey);
                if (!evaluation.IsValid) return GameContentEditOperationResult.Failure(evaluation.Reason);
            }

            if (!GameContentCollectionMutation.TryApply(
                    mapping.Descriptor,
                    currentValue.OrderedCollectionValue,
                    operation,
                    out GameContentOrderedCollectionValue proposed,
                    out string reason))
                return GameContentEditOperationResult.Failure(reason);

            GameContentFieldValue proposedValue = GameContentFieldValue.FromOrderedRecordReferenceCollection(proposed);
            if (currentValue.Equals(proposedValue))
                return GameContentEditOperationResult.Success("The staged Waves sequence is already current.");

            Dictionary<string, GameContentFieldValue> next = CopyValues(CurrentValues);
            next[mapping.Descriptor.FieldId] = proposedValue;
            if (!TryValidateReferenceValues(next, out reason))
                return GameContentEditOperationResult.Failure(reason);
            return StageValue(mapping, proposedValue, "Staged Waves collection change.");
        }

        private GameContentEditOperationResult StageValue(
            IdleAutoDefenseSerializedFieldMapping mapping,
            GameContentFieldValue value,
            string message)
        {
            if (_historyIndex < _history.Count - 1)
                _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
            Dictionary<string, GameContentFieldValue> next = CopyValues(CurrentValues);
            next[mapping.Descriptor.FieldId] = value;
            _history.Add(next);
            _historyIndex++;
            UpdateMutableState();
            return GameContentEditOperationResult.Success(message);
        }

        public GameContentEditOperationResult Undo()
        {
            if (!CanUndo) return GameContentEditOperationResult.Failure("There is no staged change to undo.");
            GameContentStaleCheckResult stale = CheckStale();
            if (stale.IsStale) return GameContentEditOperationResult.Failure(stale.Message);
            if (!TryValidateReferenceValues(_history[_historyIndex - 1], out string reason))
                return GameContentEditOperationResult.Failure(reason);
            _historyIndex--;
            UpdateMutableState();
            return GameContentEditOperationResult.Success("Undid the last staged field change.");
        }

        public GameContentEditOperationResult Redo()
        {
            if (!CanRedo) return GameContentEditOperationResult.Failure("There is no staged change to redo.");
            Dictionary<string, GameContentFieldValue> next = _history[_historyIndex + 1];
            GameContentStaleCheckResult stale = CheckStale();
            if (stale.IsStale) return GameContentEditOperationResult.Failure(stale.Message);
            if (!TryValidateReferenceValues(next, out string reason))
                return GameContentEditOperationResult.Failure(reason);
            _historyIndex++;
            UpdateMutableState();
            return GameContentEditOperationResult.Success("Redid the staged field change.");
        }

        public GameContentValidationPreview Preview()
        {
            if (_disposed) return GameContentValidationPreview.Error("Editing", "The edit session is closed.");
            if (_state == GameContentEditSessionState.Committing)
                return GameContentValidationPreview.Error("Editing", "Validation is unavailable while committing.");
            GameContentStaleCheckResult stale = CheckStale();
            if (stale.IsStale) return GameContentValidationPreview.Error("Stale Source", stale.Message);
            if (!TryValidateReferenceValues(CurrentValues, out string reason))
                return GameContentValidationPreview.Error("Authored References", reason);
            return IdleAutoDefenseProposedPackValidator.Validate(_source, CurrentValues);
        }

        public GameContentReferenceEvaluation EvaluateReferenceTarget(
            string fieldId,
            GameContentRecordKey targetKey)
        {
            if (_disposed)
                return GameContentReferenceEvaluation.Rejected(targetKey, "The edit session is closed.");
            GameContentStaleCheckResult stale = CheckStale();
            if (stale.IsStale)
                return GameContentReferenceEvaluation.Rejected(targetKey, stale.Message);
            return EvaluateReferenceTargetCore(fieldId, targetKey);
        }

        public GameContentStaleCheckResult CheckStale()
        {
            if (_disposed)
                return GameContentStaleCheckResult.Stale("The edit session is closed.", _committedRevision ?? _originalRevision);
            if (_state == GameContentEditSessionState.Committing)
                return GameContentStaleCheckResult.Current(_committedRevision ?? _originalRevision);
            if (_state == GameContentEditSessionState.RolledBack)
                return GameContentStaleCheckResult.Current(_originalRevision);

            if (!_provider.TryResolveEditableSource(_request, out IdleAutoDefenseEditableSource currentSource, out string reason))
            {
                _state = GameContentEditSessionState.Stale;
                return GameContentStaleCheckResult.Stale(reason, _committedRevision ?? _originalRevision);
            }

            if (!currentSource.SourceTarget.Equals(_source.SourceTarget))
            {
                _state = GameContentEditSessionState.Stale;
                return GameContentStaleCheckResult.Stale(
                    "The physical editable source changed after editing began. Cancel and reopen the regenerated record.",
                    _committedRevision ?? _originalRevision);
            }

            GameContentSourceRevision currentRevision;
            try
            {
                currentRevision = IdleAutoDefenseSourceRevision.Create(_request, currentSource);
            }
            catch (Exception exception)
            {
                _state = GameContentEditSessionState.Conflict;
                return GameContentStaleCheckResult.Stale(
                    "The editable source revision could not be verified: " + exception.GetBaseException().Message,
                    _committedRevision ?? _originalRevision);
            }

            GameContentSourceRevision expected = _committedRevision ?? _originalRevision;
            if (!currentRevision.Equals(expected))
            {
                _state = GameContentEditSessionState.Stale;
                return GameContentStaleCheckResult.Stale(
                    "The editable source changed after this session captured its revision. Cancel and reopen it before editing or rollback.",
                    currentRevision);
            }

            _source = currentSource;
            _state = _committedRevision == null
                ? (BuildChanges(CurrentValues).Count == 0 ? GameContentEditSessionState.Clean : GameContentEditSessionState.Dirty)
                : GameContentEditSessionState.Committed;
            return GameContentStaleCheckResult.Current(currentRevision);
        }

        public GameContentCommitResult Commit(bool confirmWarnings)
        {
            if (_disposed) return GameContentCommitResult.Failure("The edit session is closed.", _originalRevision);
            if (_state != GameContentEditSessionState.Dirty)
                return GameContentCommitResult.Failure("Only staged field changes can be committed.", _originalRevision);

            GameContentStaleCheckResult stale = CheckStale();
            if (stale.IsStale) return GameContentCommitResult.Failure(stale.Message, _originalRevision);
            GameContentValidationPreview preview = Preview();
            if (!preview.CanCommit)
                return GameContentCommitResult.Failure("The proposed authored pack is invalid. Fix the staged values before committing.", _originalRevision);
            if (preview.RequiresWarningConfirmation && !confirmWarnings)
                return GameContentCommitResult.Failure("Confirm the proposed validation warnings before committing.", _originalRevision);
            if (!_provider.TryResolveEditableSource(_request, out IdleAutoDefenseEditableSource currentSource, out string reason) ||
                !currentSource.SourceTarget.Equals(_source.SourceTarget))
                return GameContentCommitResult.Failure(string.IsNullOrWhiteSpace(reason) ? "The editable source changed before commit." : reason, _originalRevision);
            if (!IdleAutoDefenseWritableSourcePolicy.TryProbeWriteAccess(currentSource.SourcePath, out reason))
                return GameContentCommitResult.Failure(reason, _originalRevision);

            _source = currentSource;
            if (!TryValidateReferenceValues(CurrentValues, out reason))
                return GameContentCommitResult.Failure(reason, _originalRevision);
            _state = GameContentEditSessionState.Committing;
            UnityEditor.Undo.IncrementCurrentGroup();
            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            string undoName = "Edit " + _source.Record.DisplayName + " authored fields";
            UnityEditor.Undo.SetCurrentGroupName(undoName);
            try
            {
                UnityEditor.Undo.RegisterCompleteObjectUndo(_source.SourceAsset, undoName);
                IdleAutoDefenseContentEditMappings.ApplyValues(
                    _source.SourceAsset,
                    _source.Mappings,
                    CurrentValues,
                    true,
                    _source.Index);
                GameContentValidationPreview actual = _provider.ValidateActualPack(_request.RecordKey.PackId);
                if (!actual.CanCommit)
                    return RestoreFailedCommit(undoGroup, "Authoritative validation rejected the modified authored pack.");
                if (actual.RequiresWarningConfirmation && !confirmWarnings)
                    return RestoreFailedCommit(undoGroup, "Authoritative validation warnings were not confirmed.");

                EditorUtility.SetDirty(_source.SourceAsset);
                AssetDatabase.SaveAssetIfDirty(_source.SourceAsset);
                ImportSource();
                _provider.RefreshAfterExternalEdit();
                if (!_provider.TryResolveEditableSource(_request, out IdleAutoDefenseEditableSource committedSource, out reason) ||
                    !committedSource.SourceTarget.Equals(_source.SourceTarget))
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(reason) ? "The committed source could not be rebound." : reason);
                VerifyValues(committedSource, CurrentValues);
                _committedFileBytes = ReadSourceBytes(committedSource.SourcePath);
                GameContentSourceRevision committedRevision = IdleAutoDefenseSourceRevision.Create(_request, committedSource);
                if (committedRevision.Equals(_originalRevision))
                    throw new InvalidOperationException("The committed source revision did not change.");
                GameContentValidationPreview verified = _provider.ValidateActualPack(_request.RecordKey.PackId);
                if (!verified.CanCommit)
                    throw new InvalidOperationException("The persisted authored pack failed post-import validation.");

                _source = committedSource;
                _committedRevision = committedRevision;
                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                _state = GameContentEditSessionState.Committed;
                return new GameContentCommitResult(
                    true,
                    "Committed staged field values to " + _source.SourcePath + ".",
                    _originalRevision,
                    committedRevision,
                    requiresRefresh: true);
            }
            catch (Exception exception)
            {
                return RestoreFailedCommit(
                    undoGroup,
                    "Commit failed and the original values were restored: " + exception.GetBaseException().Message);
            }
        }

        public GameContentRollbackResult Rollback()
        {
            if (_disposed) return GameContentRollbackResult.Failure("The edit session is closed.", _originalRevision);
            if (_committedRevision == null)
            {
                _state = GameContentEditSessionState.RolledBack;
                return new GameContentRollbackResult(true, "Cancelled staged field changes without touching the source asset.", _originalRevision);
            }

            GameContentStaleCheckResult stale = CheckStale();
            if (stale.IsStale)
            {
                _state = GameContentEditSessionState.Stale;
                return GameContentRollbackResult.Failure(
                    "Rollback was refused because the source no longer matches the committed revision. Later changes were not overwritten.",
                    stale.CurrentRevision);
            }

            if (!_provider.TryResolveEditableSource(_request, out IdleAutoDefenseEditableSource currentSource, out string reason) ||
                !currentSource.SourceTarget.Equals(_source.SourceTarget))
                return GameContentRollbackResult.Failure(string.IsNullOrWhiteSpace(reason) ? "The committed source could not be resolved." : reason, _committedRevision);
            if (!IdleAutoDefenseWritableSourcePolicy.TryProbeWriteAccess(currentSource.SourcePath, out reason))
                return GameContentRollbackResult.Failure(reason, _committedRevision);

            _source = currentSource;
            _state = GameContentEditSessionState.Committing;
            UnityEditor.Undo.IncrementCurrentGroup();
            int undoGroup = UnityEditor.Undo.GetCurrentGroup();
            string undoName = "Rollback " + _source.Record.DisplayName + " authored fields";
            UnityEditor.Undo.SetCurrentGroupName(undoName);
            try
            {
                UnityEditor.Undo.RegisterCompleteObjectUndo(_source.SourceAsset, undoName);
                IdleAutoDefenseContentEditMappings.ApplyValues(
                    _source.SourceAsset,
                    _source.Mappings,
                    _originalValues,
                    true,
                    _source.Index);
                GameContentValidationPreview actual = _provider.ValidateActualPack(_request.RecordKey.PackId);
                if (!actual.CanCommit)
                    throw new InvalidOperationException("The restored authored pack did not pass authoritative validation.");
                RestoreSourceBytes(_originalFileBytes);
                ImportSource();
                _provider.RefreshAfterExternalEdit();
                if (!_provider.TryResolveEditableSource(_request, out IdleAutoDefenseEditableSource restoredSource, out reason) ||
                    !restoredSource.SourceTarget.Equals(_source.SourceTarget))
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(reason) ? "The restored source could not be rebound." : reason);
                VerifyValues(restoredSource, _originalValues);
                GameContentSourceRevision restoredRevision = IdleAutoDefenseSourceRevision.Create(_request, restoredSource);
                if (!restoredRevision.Equals(_originalRevision))
                    throw new InvalidOperationException("The restored source does not match the original exact revision.");
                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                _source = restoredSource;
                _state = GameContentEditSessionState.RolledBack;
                return new GameContentRollbackResult(
                    true,
                    "Restored the original authored field values in a new Unity Undo group.",
                    restoredRevision);
            }
            catch (Exception exception)
            {
                return RestoreFailedRollback(undoGroup, exception);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            IdleAutoDefenseUnityUndoBridge.UnregisterSession(this);
        }

        internal void OnUnityUndoRedo()
        {
            if (_disposed || _state == GameContentEditSessionState.Committing ||
                _state == GameContentEditSessionState.RolledBack) return;
            try
            {
                CheckStale();
            }
            catch
            {
                _state = GameContentEditSessionState.Conflict;
            }
        }

        private GameContentCommitResult RestoreFailedCommit(int undoGroup, string message)
        {
            try
            {
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                RestoreSourceBytes(_originalFileBytes);
                ImportSource();
                _provider.RefreshAfterExternalEdit();
                if (!_provider.TryResolveEditableSource(_request, out IdleAutoDefenseEditableSource restoredSource, out string reason))
                    throw new InvalidOperationException(reason);
                VerifyValues(restoredSource, _originalValues);
                GameContentSourceRevision restored = IdleAutoDefenseSourceRevision.Create(_request, restoredSource);
                if (!restored.Equals(_originalRevision))
                    throw new InvalidOperationException("The original exact source revision could not be proven after restoration.");
                _source = restoredSource;
                _state = GameContentEditSessionState.RolledBack;
                return new GameContentCommitResult(false, message, _originalRevision, restored, requiresRefresh: true);
            }
            catch (Exception recoveryException)
            {
                _state = GameContentEditSessionState.RecoveryRequired;
                GameContentRecoveryRecord recovery = BuildRecovery(
                    "Commit restoration",
                    message + " Recovery could not be proven: " + recoveryException.GetBaseException().Message,
                    _originalRevision);
                return GameContentCommitResult.Failure(recovery.ActionableMessage, _originalRevision, recovery);
            }
        }

        private GameContentRollbackResult RestoreFailedRollback(int undoGroup, Exception exception)
        {
            try
            {
                UnityEditor.Undo.RevertAllDownToGroup(undoGroup);
                if (_committedFileBytes == null)
                    throw new InvalidOperationException("The exact committed source bytes are unavailable.");
                RestoreSourceBytes(_committedFileBytes);
                ImportSource();
                _provider.RefreshAfterExternalEdit();
                if (!_provider.TryResolveEditableSource(_request, out IdleAutoDefenseEditableSource committedSource, out string reason))
                    throw new InvalidOperationException(reason);
                VerifyValues(committedSource, CurrentValues);
                GameContentSourceRevision current = IdleAutoDefenseSourceRevision.Create(_request, committedSource);
                if (!current.Equals(_committedRevision))
                    throw new InvalidOperationException("The committed revision could not be restored after rollback failure.");
                _source = committedSource;
                _state = GameContentEditSessionState.Committed;
                return GameContentRollbackResult.Failure(
                    "Rollback failed, but the committed source was restored safely: " + exception.GetBaseException().Message,
                    current);
            }
            catch (Exception recoveryException)
            {
                _state = GameContentEditSessionState.RecoveryRequired;
                GameContentRecoveryRecord recovery = BuildRecovery(
                    "Rollback restoration",
                    "Rollback failed and the committed state could not be proven: " + recoveryException.GetBaseException().Message,
                    _committedRevision);
                return GameContentRollbackResult.Failure(recovery.ActionableMessage, _committedRevision, recovery);
            }
        }

        private void ImportSource()
        {
            AssetDatabase.ImportAsset(
                _source.SourcePath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private void RestoreSourceBytes(byte[] bytes)
        {
            if (bytes == null) throw new InvalidOperationException("The exact source backup is unavailable.");
            string fullPath = IdleAutoDefenseWritableSourcePolicy.AssetPathToFullPath(_source.SourcePath);
            File.WriteAllBytes(fullPath, bytes);
            UnityEngine.Object sourceAsset = AssetDatabase.LoadMainAssetAtPath(_source.SourcePath) ?? _source.SourceAsset;
            if (sourceAsset != null) EditorUtility.ClearDirty(sourceAsset);
        }

        private static byte[] ReadSourceBytes(string sourcePath)
        {
            return File.ReadAllBytes(IdleAutoDefenseWritableSourcePolicy.AssetPathToFullPath(sourcePath));
        }

        private static void VerifyValues(
            IdleAutoDefenseEditableSource source,
            IReadOnlyDictionary<string, GameContentFieldValue> expected)
        {
            IReadOnlyDictionary<string, GameContentFieldValue> actual =
                IdleAutoDefenseContentEditMappings.ReadValues(source.SourceAsset, source.Mappings, source.Index);
            foreach (KeyValuePair<string, GameContentFieldValue> pair in expected)
            {
                if (!actual.TryGetValue(pair.Key, out GameContentFieldValue value) || !pair.Value.Equals(value))
                    throw new InvalidOperationException("Persisted field '" + pair.Key + "' does not match its staged value.");
            }
        }

        private IReadOnlyList<GameContentProposedChange> BuildChanges(
            IReadOnlyDictionary<string, GameContentFieldValue> values)
        {
            var changes = new List<GameContentProposedChange>();
            for (int i = 0; i < _source.Mappings.Count; i++)
            {
                IdleAutoDefenseSerializedFieldMapping mapping = _source.Mappings[i];
                string id = mapping.Descriptor.FieldId;
                if (!_originalValues.TryGetValue(id, out GameContentFieldValue original) ||
                    !values.TryGetValue(id, out GameContentFieldValue proposed) ||
                    original.Equals(proposed))
                    continue;
                changes.Add(new GameContentProposedChange(
                    id,
                    original,
                    proposed,
                    mapping.Descriptor.DisplayName,
                    mapping.Descriptor.Group,
                    mapping.Descriptor.Order));
            }
            return changes;
        }

        private bool TryValidateReferenceValues(
            IReadOnlyDictionary<string, GameContentFieldValue> values,
            out string reason)
        {
            foreach (IdleAutoDefenseSerializedFieldMapping mapping in _source.Mappings.Where(candidate =>
                         (candidate.Descriptor.FieldType == GameContentFieldType.RecordReference ||
                          candidate.Descriptor.FieldType == GameContentFieldType.OrderedRecordReferenceCollection) &&
                         !candidate.Descriptor.IsReadOnly))
            {
                if (!values.TryGetValue(mapping.Descriptor.FieldId, out GameContentFieldValue value))
                {
                    reason = $"The staged value for '{mapping.Descriptor.DisplayName}' is missing.";
                    return false;
                }
                if (!mapping.Descriptor.Accepts(value, out reason))
                    return false;

                if (mapping.Descriptor.FieldType == GameContentFieldType.RecordReference)
                {
                    GameContentReferenceEvaluation evaluation = EvaluateReferenceTargetCore(
                        mapping.Descriptor.FieldId,
                        value.RecordReferenceValue?.TargetKey);
                    if (!evaluation.IsValid)
                    {
                        reason = evaluation.Reason;
                        return false;
                    }
                    continue;
                }

                GameContentOrderedCollectionValue collection = value.OrderedCollectionValue;
                if (collection == null)
                {
                    reason = "The staged Waves collection is unavailable.";
                    return false;
                }
                for (int i = 0; i < collection.Count; i++)
                {
                    GameContentReferenceEvaluation evaluation = EvaluateReferenceTargetCore(
                        mapping.Descriptor.FieldId,
                        collection.Items[i].Value.RecordReferenceValue?.TargetKey);
                    if (evaluation.IsValid) continue;
                    reason = "Wave " + (i + 1) + ": " + evaluation.Reason;
                    return false;
                }
            }

            reason = string.Empty;
            return true;
        }

        private GameContentReferenceEvaluation EvaluateReferenceTargetCore(
            string fieldId,
            GameContentRecordKey targetKey)
        {
            IdleAutoDefenseSerializedFieldMapping mapping = _source.Mappings.FirstOrDefault(candidate =>
                string.Equals(candidate.Descriptor.FieldId, fieldId, StringComparison.Ordinal));
            if (mapping == null)
                return GameContentReferenceEvaluation.Rejected(targetKey, "The field is not part of this edit session.");
            if (mapping.Descriptor.FieldType == GameContentFieldType.RecordReference)
                return _provider.EvaluateAttackReferenceTarget(_source, fieldId, targetKey);
            if (mapping.Descriptor.FieldType == GameContentFieldType.OrderedRecordReferenceCollection)
                return _provider.EvaluateWaveReferenceTarget(_source, fieldId, targetKey);
            return GameContentReferenceEvaluation.Rejected(targetKey, "The field does not accept canonical record references.");
        }

        private void UpdateMutableState()
        {
            _state = BuildChanges(CurrentValues).Count == 0
                ? GameContentEditSessionState.Clean
                : GameContentEditSessionState.Dirty;
        }

        private GameContentRecoveryRecord BuildRecovery(
            string phase,
            string message,
            GameContentSourceRevision currentRevision)
        {
            return new GameContentRecoveryRecord(
                BackendId,
                SourceTarget.LockKey,
                SourceTarget.SourceLabel,
                _originalRevision,
                currentRevision,
                DateTime.UtcNow,
                phase,
                "Review '" + _source.SourcePath + "' before editing again. " + message);
        }

        private static Dictionary<string, GameContentFieldValue> CopyValues(
            IReadOnlyDictionary<string, GameContentFieldValue> source)
        {
            var copy = new Dictionary<string, GameContentFieldValue>(StringComparer.Ordinal);
            if (source == null) return copy;
            foreach (KeyValuePair<string, GameContentFieldValue> pair in source)
                if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value != null) copy[pair.Key] = pair.Value;
            return copy;
        }
    }
}
