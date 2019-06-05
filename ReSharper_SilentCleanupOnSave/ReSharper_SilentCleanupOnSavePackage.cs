using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace ReSharper_SilentCleanupOnSave
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [ PackageRegistration( UseManagedResourcesOnly = true, AllowsBackgroundLoading = true ) ]
    [ InstalledProductRegistration( Name, Description, Version ) ]
    [ Guid( PackageGuidString ) ]
    [ ProvideAutoLoad( VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad ) ]
    public sealed class ReSharper_SilentCleanupOnSavePackage : AsyncPackage
    {
        #region Public Fields

        public const string Description = "Automatically runs ReSharper's Silent Cleanup command on Save.";
        public const string Name = "ReSharper_SilentCleanupOnSave";
        public const string PackageGuidString = "6BE0E63C-D9C8-4FE8-AC32-1D3099D4A3EB";
        public const string Version = "0.1";

        #endregion

        #region Private Fields

        private static readonly string[] AllowedFileExtensions =
            { ".cs", ".xaml", ".vb", ".js", ".ts", ".css", ".html", ".xml", ".json", ".cpp", ".h", ".c" };

        private static readonly string[] ReSharperSilentCleanupCodeCommandsNames =
            { "ReSharper.ReSharper_SilentCleanupCode", "ReSharper_SilentCleanupCode" };

        private Document _activeDocument;
        private Command _cleanupCommand;
        private BuildEvents _buildEvents;
        private bool _building;
        private bool _disposed;
        private DocumentEvents _docEvents;
        private DTE _dte;
        private bool _solutionClosing;
        private SolutionEvents _solutionEvents;

        #endregion

        #region Public Constructors

        public ReSharper_SilentCleanupOnSavePackage()
        {
            Application.ThreadException += ApplicationOnThreadException;
            Application.SetUnhandledExceptionMode( UnhandledExceptionMode.CatchException );

            ShowDebugMessage( "Constructor" );
        }

        #endregion

        #region Protected Methods

        protected override void Dispose( bool disposing )
        {
            if ( disposing && !_disposed )
            {
                _disposed = true;

                if ( _docEvents != null )
                {
                    _docEvents.DocumentSaved -= DocEventsOnDocumentSaved;
                    _docEvents = null;
                }

                if ( _buildEvents != null )
                {
                    _buildEvents.OnBuildBegin += BuildEventsOnOnBuildBegin;
                    _buildEvents.OnBuildDone += BuildEventsOnOnBuildDone;
                    _buildEvents = null;
                }

                if ( _solutionEvents != null )
                {
                    _solutionEvents.BeforeClosing -= SolutionEventsOnBeforeClosing;
                    _solutionEvents = null;
                }
            }

            base.Dispose( disposing );
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress
        )
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.

            ShowDebugMessage( "InitializeAsync - enter" );

            await JoinableTaskFactory.SwitchToMainThreadAsync( cancellationToken );

            _dte = ( DTE ) GetGlobalService( typeof( DTE ) );

            _docEvents = _dte.Events.DocumentEvents;
            _docEvents.DocumentSaved += DocEventsOnDocumentSaved;

            _buildEvents = _dte.Events.BuildEvents;
            _buildEvents.OnBuildBegin += BuildEventsOnOnBuildBegin;
            _buildEvents.OnBuildDone += BuildEventsOnOnBuildDone;

            _solutionEvents = _dte.Events.SolutionEvents;
            _solutionEvents.BeforeClosing += SolutionEventsOnBeforeClosing;

            _cleanupCommand = _dte.Commands.Cast<Command>().FirstOrDefault( x => ReSharperSilentCleanupCodeCommandsNames.Contains( x.Name ) );

            ShowDebugMessage( $"Command: {_cleanupCommand?.Name}" );
            ShowDebugMessage( "InitializeAsync - exit" );
        }

        private void SolutionEventsOnBeforeClosing() => _solutionClosing = true;

        #endregion

        #region Private Methods

        private void ApplicationOnThreadException( object sender, ThreadExceptionEventArgs e ) => ShowError( e?.Exception?.ToString() ?? "Unknown Error" );

        private void BuildEventsOnOnBuildBegin( vsBuildScope scope, vsBuildAction action ) => _building = true;

        private void BuildEventsOnOnBuildDone( vsBuildScope scope, vsBuildAction action ) => _building = false;

        private void DocEventsOnDocumentSaved( Document document )
        {
            try
            {
                bool IsMiscDoc( Document doc ) => ( doc.ProjectItem.ContainingProject?.Name ?? "Miscellaneous Files" ) == "Miscellaneous Files";

                if (
                    _solutionClosing
                    || _building
                    || !( _cleanupCommand?.IsAvailable ?? false )
                    || document.ReadOnly
                    || IsMiscDoc( document )
                    || !AllowedFileExtensions.Contains( Path.GetExtension( document.FullName )?.ToLower() )
                ) return;

                _activeDocument = _dte.ActiveDocument;

                document.Activate();

                _dte.ExecuteCommand( _cleanupCommand.Name );

                _activeDocument.Activate();
            }
            catch ( Exception e )
            {
                ShowDebugError( e.ToString() );
            }
        }

        private void ShowDebugError( string message )
        {
#if DEBUG
            ShowMessageBox( message, MessageBoxIcon.Error );
#endif
        }

        private void ShowDebugMessage( string message )
        {
#if DEBUG
            ShowMessageBox( message );
#endif
        }

        private void ShowError( string message )
        {
            ShowMessageBox( message, MessageBoxIcon.Error );
        }

        private void ShowMessage( string message ) => ShowMessageBox( message );

        private DialogResult ShowMessageBox(
            string message,
            MessageBoxIcon messageBoxIcon = MessageBoxIcon.Information,
            MessageBoxDefaultButton messageBoxDefaultButton = MessageBoxDefaultButton.Button1,
            MessageBoxButtons messageBoxButtons = MessageBoxButtons.OK
        )
        {
            return MessageBox.Show(
                message,
                Name,
                messageBoxButtons,
                messageBoxIcon,
                messageBoxDefaultButton
            );
        }

        #endregion
    }
}
