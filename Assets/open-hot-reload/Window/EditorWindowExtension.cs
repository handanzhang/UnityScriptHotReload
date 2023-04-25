using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ScriptHotReload
{
	public static class EditorWindowExtension
	{
		public static object GetParentOf(object target)
		{
			var field = target.GetType().GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field == null)
			{
				var property = target.GetType().GetProperty("parent", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public);
				return property.GetValue(target);
			}
			else
			{
				return field.GetValue(target);			
			}
		}
	}
    public static class Docker
    {
        // Helper to dock EditorWindows
        // Simulate a mouse drop to dock window.
        // ref: https://gist.github.com/Thundernerd/5085ec29819b2960f5ff2ee32ad57cbb
        
        // SplitView->DockArea->EditorWindow
        public enum DockPosition
		{
			Left,
			Top,
			Right,
			Bottom,
			RightBottom,
		}

		private static Vector2 GetFakeMousePosition(Rect rect, DockPosition position)
		{
			Vector2 mousePosition = Vector2.zero;

			// The 20 is required to make the docking work.
			// Smaller values might not work when faking the mouse position.
			switch(position)
			{
			case DockPosition.Left: mousePosition = new Vector2(20, rect.size.y / 2); break;
			case DockPosition.Top: mousePosition = new Vector2(rect.size.x / 2, 20); break;
			case DockPosition.Right: mousePosition = new Vector2(rect.size.x - 20, rect.size.y / 2); break;
			case DockPosition.Bottom: mousePosition = new Vector2(rect.size.x / 2, rect.size.y - 20); break;
			case DockPosition.RightBottom: mousePosition = new Vector2(rect.size.x - 20, rect.size.y - 20); break;
			}

			return new Vector2(rect.x + mousePosition.x, rect.y + mousePosition.y);
		}

		/// <summary>
		/// Docks the "docked" window to the "anchor" window at the given position
		/// </summary>
		public static void DockWindow(this EditorWindow anchor, EditorWindow docked, DockPosition position)
		{
			if (IsWindowInSameSplitView(anchor, docked))
			{
				Debug.Log("[Docker] The target window is already docked.");
				return;
			}
			// anchorParent is DockArea or null
			var anchorParent = EditorWindowExtension.GetParentOf(anchor);

			SetDragSource(anchorParent, EditorWindowExtension.GetParentOf(docked));
			PerformDrop(SplitViewExtension.GetSplitView(anchorParent), docked, GetFakeMousePosition(anchor.position, position));
		}
		
		static bool IsWindowInSameSplitView(this EditorWindow window, EditorWindow otherWindow)
		{
			var selfDockArea = EditorWindowExtension.GetParentOf(window);
			var otherDockArea = EditorWindowExtension.GetParentOf(otherWindow);
			if (selfDockArea != null && selfDockArea.GetType().Name == "DockArea" && otherDockArea != null &&
			    otherDockArea.GetType().Name == "DockArea")
			{
				return EditorWindowExtension.GetParentOf(selfDockArea) == EditorWindowExtension.GetParentOf(otherDockArea);
			}

			return false;
		}

		static void SetDragSource(object target, object source)
		{
			var field = target.GetType().GetField("s_OriginalDragSource", BindingFlags.Static | BindingFlags.NonPublic);
			field.SetValue(null, source);
		}
		
		static void PerformDrop(object rootSplitView, EditorWindow child, Vector2 screenPoint, bool useFixedWidth=true)
		{
			var dragMethod = rootSplitView.GetType().GetMethod("DragOver", BindingFlags.Instance | BindingFlags.Public);
			var dropMethod = rootSplitView.GetType().GetMethod("PerformDrop", BindingFlags.Instance | BindingFlags.Public);

			var dropInfo = dragMethod.Invoke(rootSplitView, new object[] { child, screenPoint });
			if (dropInfo == null)
			{
				Debug.LogWarning("[Docker] Dock EditorWindow failed because of the invalid dropInfo.");
				return;
			}

			if (useFixedWidth)
			{
				var rectField = dropInfo.GetType().GetField("rect", BindingFlags.Instance | BindingFlags.Public);
				var rect = rectField.GetValue(dropInfo);
				if(rect is Rect r)
				{
					r.width = child.position.width;
					rectField.SetValue(dropInfo, r);
				}	
			}
			dropMethod.Invoke(rootSplitView, new object[] { child, dropInfo, screenPoint });
		}

		public static void AddTab(this EditorWindow window, EditorWindow windowAddToTab)
		{
			var selfDockArea = EditorWindowExtension.GetParentOf(window);
			var addTab = selfDockArea.GetType().GetMethod("AddTab", new Type[]{typeof(EditorWindow), typeof(bool)}); 
			addTab?.Invoke(selfDockArea, new object[] {windowAddToTab, true});
		}
    }

    public static class SplitViewExtension
    {
	    public static object GetSplitView(object target)
	    {
		    var parentProperty = target.GetType().GetProperty("parent", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public);
		    if (parentProperty != null)
		    {
			    var parent = parentProperty.GetValue(target);
			    if (parent.GetType().Name == "SplitView")
			    {
				    return parent;
			    }
		    }
		    var property = target.GetType().GetProperty("window", BindingFlags.Instance | BindingFlags.Public);
		    var window = property.GetValue(target, null);
		    var rootSplitViewProperty = window.GetType().GetProperty("rootSplitView", BindingFlags.Instance | BindingFlags.Public);
		    object rootSplitView = rootSplitViewProperty.GetValue(window, null);
		    return rootSplitView;
	    }
	    
	    public static void SetupSplitter(EditorWindow target, int[] realSizes)
	    {
		    var parent = EditorWindowExtension.GetParentOf(target);
		    var property = parent.GetType().GetProperty("window", BindingFlags.Instance | BindingFlags.Public);
		    var window = property.GetValue(parent, null);
		    
		    var rootSplitViewProperty = window.GetType().GetProperty("rootSplitView", BindingFlags.Instance | BindingFlags.Public);
		    object rootSplitView = rootSplitViewProperty.GetValue(window, null);
		    if (rootSplitView != null)
		    {
			    var field = rootSplitView.GetType().GetField("splitState", BindingFlags.Instance | BindingFlags.NonPublic);
			    if (field != null)
			    {
				    var splitState = field.GetValue(rootSplitView);
				    var fieldRelativeSizes = splitState.GetType().GetField("realSizes", BindingFlags.Instance | BindingFlags.Public);
				    fieldRelativeSizes?.SetValue(splitState, realSizes);
			    }
			    var methodSetupRectsFromSplitter = rootSplitView.GetType().GetMethod("SetupRectsFromSplitter", BindingFlags.Instance | BindingFlags.NonPublic);
			    methodSetupRectsFromSplitter?.Invoke(rootSplitView, new object[]{});
		    }
	    }
    }
}