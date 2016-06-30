﻿//------------------------------------------------------------------------------
// <copyright file="ToggleComment.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Operations;
using OleInterop = Microsoft.VisualStudio.OLE.Interop;

namespace HotCommands
{
    /// <summary>
    /// Command handler for ToggleComment
    /// </summary>
    internal sealed class ToggleComment
    {
        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToggleComment"/> class.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ToggleComment(Package package)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            this.package = package;
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ToggleComment Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
                Instance = new ToggleComment(package);
        }

        public int HandleCommand(IWpfTextView textView, IClassifier classifier, OleInterop.IOleCommandTarget commandTarget, IEditorOperations editorOperations)
        {
            Guid cmdGroup = VSConstants.VSStd2K;

            // Is anything selected? (Or just a cursor)
            bool cursorOnly = IsCursorOnly(textView);

            // Execute Comment or Uncomment depending on current state of selected code
            uint cmdID = IsAllCommented(textView, classifier) ? (uint) VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK : (uint) VSConstants.VSStd2KCmdID.COMMENT_BLOCK;
            int hr = commandTarget.Exec(ref cmdGroup, cmdID, (uint)OleInterop.OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, IntPtr.Zero, IntPtr.Zero);

            if (cursorOnly)
            {
                // Move caret down one line
                editorOperations.MoveLineDown(extendSelection:false);
            }

            return VSConstants.S_OK;
        }

        private bool IsCursorOnly(IWpfTextView textView)
        {
            if (textView.Selection.SelectedSpans.Count > 1) return false;
            // Only one selection. Check if there is any selected content.
            return textView.Selection.SelectedSpans[0].Length == 0;
        }

        private bool IsAllCommented(IWpfTextView textView, IClassifier classifier)
        {
            foreach (SnapshotSpan snapshotSpan in textView.Selection.SelectedSpans)
            {
                SnapshotSpan spanToCheck = snapshotSpan.Length == 0 ?
                    new SnapshotSpan(textView.TextSnapshot, textView.Caret.ContainingTextViewLine.Extent.Span) :
                    snapshotSpan;
                IList<ClassificationSpan> classificationSpans = classifier.GetClassificationSpans(spanToCheck);
                foreach (var classification in classificationSpans)
                {
                    var name = classification.ClassificationType.Classification.ToLower();
                    if (!name.Contains(PredefinedClassificationTypeNames.Comment))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

    }
}
