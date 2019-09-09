using System;
using System.Collections.Generic;
using System.Text;
using MiniIPC.Attributes;

namespace RuntimeUnityEditor.Common.Service
{
    [MiniService]
    public interface IRuntimeUnityEditorService
    {
        void Echo(string message);
    }
}
