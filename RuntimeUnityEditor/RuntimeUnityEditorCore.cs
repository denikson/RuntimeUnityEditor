using System;
using System.IO;
using System.Reflection;
using System.Threading;
using GhettoPipes;
using MessagePack.Resolvers;
using MiniIPC.Service;
using RuntimeUnityEditor.Common.Service;
using RuntimeUnityEditor.Core.Gizmos;
using RuntimeUnityEditor.Core.ObjectTree;
using RuntimeUnityEditor.Core.REPL;
using RuntimeUnityEditor.Core.UI;
using RuntimeUnityEditor.Core.Utils;
using UnityEngine;

namespace RuntimeUnityEditor.Core
{
    public class Service : IRuntimeUnityEditorService
    {
        public void Echo(string message)
        {
            RuntimeUnityEditorCore.Logger.Log(LogLevel.Info, message);
        }
    }

    public class RuntimeUnityEditorCore
    {
        public const string Version = "1.8";
        public const string GUID = "RuntimeUnityEditor";

        public Inspector.Inspector Inspector { get; }
        public ObjectTreeViewer TreeViewer { get; }
        public ReplWindow Repl { get; }

        public KeyCode ShowHotkey { get; set; } = KeyCode.F12;

        internal static RuntimeUnityEditorCore Instance { get; private set; }
        internal static MonoBehaviour PluginObject { get; private set; }
        internal static ILoggerWrapper Logger { get; private set; }

        internal static GizmoDrawer GizmoDrawer { get; private set; }

        private CursorLockMode _previousCursorLockState;
        private bool _previousCursorVisible;
        private StreamServiceReceiver<IRuntimeUnityEditorService> serviceReceiver;
        private NamedPipeStream receivePipeStream;
        private Thread receiveThread;

        internal RuntimeUnityEditorCore(MonoBehaviour pluginObject, ILoggerWrapper logger, string configPath)
        {
            if (Instance != null)
                throw new InvalidOperationException("Can only create one instance of the Core object");

            CompositeResolver.RegisterAndSetAsDefault(GeneratedResolver.Instance, BuiltinResolver.Instance, PrimitiveObjectResolver.Instance);

            PluginObject = pluginObject;
            Logger = logger;
            Instance = this;

            InitNativeGUI();

            Inspector = new Inspector.Inspector(targetTransform => TreeViewer.SelectAndShowObject(targetTransform));

            TreeViewer = new ObjectTreeViewer(pluginObject);
            TreeViewer.InspectorOpenCallback = items =>
            {
                Inspector.InspectorClear();
                foreach (var stackEntry in items)
                    Inspector.InspectorPush(stackEntry);
            };

            if (UnityFeatureHelper.SupportsVectrosity)
            {
                GizmoDrawer = new GizmoDrawer(pluginObject);
                TreeViewer.TreeSelectionChangedCallback = transform => GizmoDrawer.UpdateState(transform);
            }

            if (UnityFeatureHelper.SupportsCursorIndex &&
                UnityFeatureHelper.SupportsXml)
            {
                try
                {
                    Repl = new ReplWindow(Path.Combine(configPath, "RuntimeUnityEditor.Autostart.cs"));
                    Repl.RunAutostart();
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Warning, "Failed to load REPL - " + ex.Message);
                }
            }
        }

        private delegate void ShowGUIDelegate();

        private ShowGUIDelegate ShowGUI;

        private void InitNativeGUI()
        {
            var libPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                                       "RuntimeUnityEditor.UI.dll");

            Logger.Log(LogLevel.Info, $"Lib path: {libPath}");
            var lib = NativeUtils.LoadNativeLibrary(libPath);

            if (lib == null)
            {
                Logger.Log(LogLevel.Error, "No UI library found!");
                return;
            }

            ShowGUI = lib.GetFunctionAsDelegate<ShowGUIDelegate>(nameof(ShowGUI));

            receivePipeStream = NamedPipeStream.Create("RuntimeUnityEditor_Service", NamedPipeStream.PipeDirection.InOut, securityDescriptor: "D:(A;OICI;GA;;;WD)");
            serviceReceiver = new StreamServiceReceiver<IRuntimeUnityEditorService>(new Service(), receivePipeStream);
            receiveThread = new Thread(ReceiveLoop);
            receiveThread.Start();
        }

        private void ReceiveLoop()
        {
            Logger.Log(LogLevel.Info, "Waiting for connection...");
            receivePipeStream.WaitForConnection();
            Logger.Log(LogLevel.Info, "Got connection! Processing messages!");

            while (true)
            {
                serviceReceiver.ProcessMessage();
                receivePipeStream.Flush();
            }
        }

        internal void OnGUI()
        {
            if (Show)
            {
                var originalSkin = GUI.skin;
                GUI.skin = InterfaceMaker.CustomSkin;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                Inspector.DisplayInspector();
                TreeViewer.DisplayViewer();
                Repl?.DisplayWindow();

                // Restore old skin for maximum compatibility
                GUI.skin = originalSkin;
            }
        }

        public bool Show
        {
            get => TreeViewer.Enabled;
            set
            {
                if (Show != value)
                {
                    if (value)
                    {
                        _previousCursorLockState = Cursor.lockState;
                        _previousCursorVisible = Cursor.visible;
                    }
                    else
                    {
                        Cursor.lockState = _previousCursorLockState;
                        Cursor.visible = _previousCursorVisible;
                    }
                }

                TreeViewer.Enabled = value;

                if (GizmoDrawer != null)
                {
                    GizmoDrawer.Show = value;
                    GizmoDrawer.UpdateState(TreeViewer.SelectedTransform);
                }

                if (value)
                {
                    SetWindowSizes();

                    TreeViewer.UpdateCaches();
                }
            }
        }

        internal void Update()
        {
            if (Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Logger.Log(LogLevel.Info, "Starting GUI");
                ShowGUI();
                return;
            }

            if (Input.GetKeyDown(ShowHotkey))
                Show = !Show;

            Inspector.InspectorUpdate();
        }

        private void SetWindowSizes()
        {
            const int screenOffset = 10;

            var screenRect = new Rect(
                screenOffset,
                screenOffset,
                Screen.width - screenOffset * 2,
                Screen.height - screenOffset * 2);

            var centerWidth = (int)Mathf.Min(850, screenRect.width);
            var centerX = (int)(screenRect.xMin + screenRect.width / 2 - Mathf.RoundToInt((float)centerWidth / 2));

            var inspectorHeight = (int)(screenRect.height / 4) * 3;
            Inspector.UpdateWindowSize(new Rect(
                centerX,
                screenRect.yMin,
                centerWidth,
                inspectorHeight));

            var rightWidth = 350;
            var treeViewHeight = screenRect.height;
            TreeViewer.UpdateWindowSize(new Rect(
                screenRect.xMax - rightWidth,
                screenRect.yMin,
                rightWidth,
                treeViewHeight));

            var replPadding = 8;
            Repl?.UpdateWindowSize(new Rect(
                centerX,
                screenRect.yMin + inspectorHeight + replPadding,
                centerWidth,
                screenRect.height - inspectorHeight - replPadding));
        }
    }
}
