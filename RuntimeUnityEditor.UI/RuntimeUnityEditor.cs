using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GhettoPipes;
using MiniIPC.Service;
using RuntimeUnityEditor.Common.Service;

namespace RuntimeUnityEditor.UI
{
    public static class RuntimeUnityEditor
    {
        public static bool isInitialized = false;

        public static IRuntimeUnityEditorService Service { get; private set; }
        private static StreamServiceSender<IRuntimeUnityEditorService> serviceSender;
        private static NamedPipeStream servicePipe;

        public static void InitializeConnection()
        {
            if (isInitialized)
                return;
            servicePipe = NamedPipeStream.Open("RuntimeUnityEditor_Service", NamedPipeStream.PipeDirection.InOut);
            serviceSender = new StreamServiceSender<IRuntimeUnityEditorService>(servicePipe);
            Service = serviceSender.Service;
            isInitialized = true;

            //if (serviceSender != null)
            //    return "";

            //try
            //{

            //    return "";
            //}
            //catch (Exception e)
            //{
            //    return e.Message;
            //}
        }
    }
}
