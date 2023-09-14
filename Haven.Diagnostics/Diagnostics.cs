
using System.Xml.Serialization;




//	internal static class DiagnosticsHandler
//	{
//		//internal XmlSerializer PipeSerializer;
//	
//	
//	}
//	
//	public static class DiagMessage
//	{
//		public const byte Success = 0;
//	
//		public const byte GetDiagInfo = 1;
//	
//	
//		public const byte ErrorNotFound = 99;
//	}
//	
//	public struct DiagInfo
//	{
//		[XmlAttribute("MaxLayerCount")]
//		public int MaxLayerCount;
//	
//		[XmlAttribute("FPS")]
//		public int FPS;
//	
//		[XmlElement("Layer")]
//		public List<LayerInfo> Layers;
//	
//		public DiagInfo()
//		{
//			Layers = new();
//		}
//	
//		public static DiagInfo FromCurrentState()
//		{
//			DiagInfo d = new();
//	
//			d.MaxLayerCount = App.Instance.ActiveLayers.Length;
//			d.FPS = App.Instance.CurrentFPS;
//	
//			foreach (var kvp in App.Instance.Layers)
//			{
//				bool Active = App.Instance.IsLayerVisible(kvp.Key);
//				int Index = -1;
//	
//				if (Active)
//				{
//					for (int i = 0; i < App.Instance.ActiveLayers.Length; i++)
//						if (App.Instance.ActiveLayers[i] == kvp.Value)
//							Index = i;
//				}
//	
//				LayerInfo info = new();
//	
//				info.Name = kvp.Key;
//				info.Active = Active;
//				info.Index = Index;
//	
//				foreach (Widget w in kvp.Value.Widgets)
//					info.Widgets.Add(w.GetType().Name);
//	
//				foreach (var ut in kvp.Value.UpdateTasks)
//					info.UpdateTasks.Add(ut.Method.Name);
//	
//				d.Layers.Add(info);
//			}
//	
//			return d;
//		}
//	}
//	
//	public struct LayerInfo
//	{
//		[XmlAttribute("Name")]
//		public string Name;
//	
//		[XmlAttribute("Active")]
//		public bool Active;
//	
//		[XmlAttribute("Index")]
//		public int Index;
//	
//		[XmlElement("Widget")]
//		public List<string> Widgets;
//	
//		[XmlElement("UpdateTask")]
//		public List<string> UpdateTasks;
//	
//		public LayerInfo()
//		{
//			Widgets = new();
//			UpdateTasks = new();
//		}
//	}