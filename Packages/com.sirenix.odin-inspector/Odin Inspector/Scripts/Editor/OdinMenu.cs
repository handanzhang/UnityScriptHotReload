using System;
using UnityEngine;

namespace Sirenix.OdinInspector.Editor
{
    public abstract class OdinMenu
    {
        public abstract string MenuPath();
        public abstract Texture Icon();

        public virtual int Order()
        {
            return int.MaxValue;
        }

        public virtual void OnEnable()
        {
        }

        public virtual void OnInspectorUpdate()
        {
        }

        public virtual void OnDisable()
        {
        }

        public virtual bool ShouldShow()
        {
            return true;
        }

        public static bool NeedRebuildMenuTree;
        public static bool autoRepaintOnSceneChange;
    }

    public class ArtistMenu : Attribute
    {
    }

    public class TechMenu : Attribute
    {
    }

    public class DesignerMenu : Attribute
    {
    }
}

