// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;
using OutGridView.Models;
using System.Runtime.InteropServices;

namespace OutGridView.Cmdlet
{
    /// Enum for SelectionMode parameter.
    /// </summary>
    [Cmdlet(VerbsData.Out, "ConsoleGridView", DefaultParameterSetName = "PassThru")]
    [Alias("ocgv")]
    public class OutConsoleGridViewCmdletCommand : PSCmdlet, IDisposable
    {
        #region Properties

        private const string DataNotQualifiedForGridView = "DataNotQualifiedForGridView";
        private const string OSNotSupportedForGridView = "OSNotSupportedForGridView";

        private List<PSObject> _psObjects = new List<PSObject>();
        private ConsoleGui _consoleGui = new ConsoleGui();

        #endregion Properties

        #region Input Parameters

        /// <summary>
        /// This parameter specifies the current pipeline object.
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { get; set; } = AutomationNull.Value;

        /// <summary>
        /// Gets/sets the title of the Out-GridView window.
        /// </summary>
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string Title { get; set; }

        /// <summary>
        /// Get or sets a value indicating whether the selected items should be written to the pipeline
        /// and if it should be possible to select multiple or single list items.
        /// </summary>
        [Parameter(ParameterSetName = "OutputMode")]
        public OutputModeOption OutputMode { set; get; }

        /// <summary>
        /// Gets or sets a value indicating whether the selected items should be written to the pipeline.
        /// Setting this to true is the same as setting the OutputMode to Multiple.
        /// </summary>
        [Parameter(ParameterSetName = "PassThru")]
        public SwitchParameter PassThru
        {
            set { this.OutputMode = value.IsPresent ? OutputModeOption.Multiple : OutputModeOption.None; }

            get { return OutputMode == OutputModeOption.Multiple ? new SwitchParameter(true) : new SwitchParameter(false); }
        }

        #endregion Input Parameters

        // This method gets called once for each cmdlet in the pipeline when the pipeline starts executing
        protected override void BeginProcessing()
        {
        }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            if (InputObject == null || InputObject == AutomationNull.Value)
            {
                return;
            }

            IDictionary dictionary = InputObject.BaseObject as IDictionary;
            if (dictionary != null)
            {
                // Dictionaries should be enumerated through because the pipeline does not enumerate through them.
                foreach (DictionaryEntry entry in dictionary)
                {
                    ProcessObject(PSObject.AsPSObject(entry));
                }
            }
            else
            {
                ProcessObject(InputObject);
            }
        }

        private void ProcessObject(PSObject input)
        {

            object baseObject = input.BaseObject;

            // Throw a terminating error for types that are not supported.
            if (baseObject is ScriptBlock ||
                baseObject is SwitchParameter ||
                baseObject is PSReference ||
                baseObject is PSObject)
            {
                ErrorRecord error = new ErrorRecord(
                    new FormatException("Invalid data type for Out-GridView"),
                    DataNotQualifiedForGridView,
                    ErrorCategory.InvalidType,
                    null);

                this.ThrowTerminatingError(error);
            }

            _psObjects.Add(input);
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {
            base.EndProcessing();

            //Return if no objects
            if (_psObjects.Count == 0)
            {
                return;
            }

            var TG = new TypeGetter(this);

            var dataTable = TG.CastObjectsToTableView(_psObjects);
            var applicationData = new ApplicationData
            {
                Title = Title ?? "Out-ConsoleGridView",
                OutputMode = OutputMode,
                PassThru = PassThru,
                DataTable = dataTable
            };


            _consoleGui.Start(applicationData);

            // Don't write anything out to the pipeline if PassThru wasn't specified.
            if (!PassThru.IsPresent)
            {
                return;
            }

            var selectedIndexes = _consoleGui.SelectedIndexes;

            foreach (int idx in selectedIndexes)
            {
                var selectedObject = _psObjects[idx];
                if (selectedObject == null)
                {
                    continue;
                }
                this.WriteObject(selectedObject, false);
            }
        }

        public void Dispose()
        {
            _consoleGui.Dispose();
        }
    }
}
