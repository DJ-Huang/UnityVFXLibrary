﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using System.IO;
using System.Security.AccessControl;

namespace LWGUI
{
	/// <summary>
	/// Create a Folding Group
	/// group：group name (Default: Property Name)
	/// keyword：keyword used for toggle, "_" = ignore, none or "__" = Property Name +  "_ON", always Upper (Default: none)
	/// default Folding State: "on" or "off" (Default: off)
	/// default Toggle Displayed: "on" or "off" (Default: on)
	/// Target Property Type: FLoat, express Toggle value
	/// </summary>
	public class MainDrawer : MaterialPropertyDrawer
	{
		private bool   _isFolding;
		private string _group;
		private string _keyword;
		private bool   _defaultFoldingState;
		private bool   _defaultToggleDisplayed;

		public MainDrawer() : this("") { }

		public MainDrawer(string group) : this(group, "") { }

		public MainDrawer(string group, string keyword) : this(group, keyword, "off") { }

		public MainDrawer(string group, string keyword, string defaultFoldingState) : this(group, keyword, defaultFoldingState, "on") { }

		// Obsolete
		public MainDrawer(string group, string keyword, float style) : this(group, keyword, (style == 1 || style == 3) ? "on" : "off", (style == 0 || style == 1) ? "on" : "off")
		{
			Helper.ObsoleteWarning("MainDrawer(string group, string keyword, float style)",
								   "MainDrawer(string group, string keyword, string defaultFoldingState, string defaultToggleDisplayed)");
		}

		public MainDrawer(string group, string keyword, string defaultFoldingState, string defaultToggleDisplayed)
		{
			this._group = group;
			this._keyword = keyword;
			this._defaultFoldingState = defaultFoldingState == "on";
			this._defaultToggleDisplayed = defaultToggleDisplayed == "on";
		}

		public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			EditorGUI.showMixedValue = prop.hasMixedValue;

			var toggleValue = prop.floatValue == 1.0f;
			string finalGroupName = _group != "" ? _group : prop.name;
			bool isFirstFrame = !GUIData.group.ContainsKey(finalGroupName);
			_isFolding = isFirstFrame ? !_defaultFoldingState : GUIData.group[finalGroupName];

			bool toggleResult = Helper.Foldout(position, ref _isFolding, toggleValue, _defaultToggleDisplayed, label.text);
			EditorGUI.showMixedValue = false;

			if (toggleResult != toggleValue)
			{
				prop.floatValue = toggleResult ? 1.0f : 0.0f;
				Helper.SetShaderKeyWord(editor.targets, Helper.GetKeyWord(_keyword, prop.name), toggleResult);
			}
			// Sometimes the Toggle is activated but the key is not activated
			else
			{
				if (!prop.hasMixedValue)
					Helper.SetShaderKeyWord(editor.targets, Helper.GetKeyWord(_keyword, prop.name), toggleResult);
			}

			if (isFirstFrame)
				GUIData.group.Add(finalGroupName, _isFolding);
			else
				GUIData.group[finalGroupName] = _isFolding;
		}

		// If use OnGUI(Rect position) as Rect, we need to accurately set GetPropertyHeight()
		// if use EditorGUILayout.GetControlRect() as Rect, GetPropertyHeight() only increases the interval
		public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
		{
			return 4f;
		}
	}

	/// <summary>
	/// Draw a property with default style in the folding group
	/// group：father group name, support suffix keyword for conditional display (Default: none)
	/// Target Property Type: Any
	/// </summary>
	public class SubDrawer : MaterialPropertyDrawer
	{
		public static readonly int propRight = 80;

		protected string             group = "";
		protected MaterialProperty   prop;
		protected MaterialProperty[] props;

		protected bool IsVisible() { return Helper.IsVisible(group); }

		protected virtual float GetVisibleHeight()   { return 0; }
		protected virtual float GetInvisibleHeight() { return -2; } // Remove the redundant height when invisible

		protected virtual bool IsMatchPropType() { return true; }

		public SubDrawer() { }

		public SubDrawer(string group)
		{
			this.group = group;
		}

		public override void OnGUI(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			this.prop = prop;
			props = Helper.GetProperties(editor);
			
			if (group != "" && group != "_")
			{
				EditorGUI.indentLevel++;
				if (IsVisible())
				{
					if (IsMatchPropType())
						DrawProp(position, prop, label, editor);
					else
					{
						Debug.LogWarning(
										 this.GetType() + " does not support this MaterialProperty type:'" + prop.type + "'!");
						editor.DefaultShaderProperty(prop, label.text);
					}
				}

				EditorGUI.indentLevel--;
			}
			else
			{
				if (IsMatchPropType())
					DrawProp(position, prop, label, editor);
				else
				{
					Debug.LogWarning(this.GetType() + " does not support this MaterialProperty type:'" + prop.type + "'!");
					editor.DefaultShaderProperty(prop, label.text);
				}
			}
		}

		public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
		{
			return IsVisible() || group == "" || group == "_" ? GetVisibleHeight() : GetInvisibleHeight();
		}

		// Draws a custom style property
		public virtual void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			editor.DefaultShaderProperty(prop, label.text);
		}
	}

	public class SurfaceDrawer : SubDrawer
	{
		private enum SurfaceEnum
		{
			Opaque = 0,
			Transparent = 1,
			TransparentCutout = 2,
		}

		private SurfaceEnum surfaceEnum = SurfaceEnum.Opaque;
		
		#region
		
		public SurfaceDrawer(string group)
		{
			this.group = group;
		}

		#endregion
		
		protected override bool IsMatchPropType() { return prop.type == MaterialProperty.PropType.Float; }

		public static readonly string[] surfaceTypeNames = Enum.GetNames(typeof(SurfaceEnum));

		public override void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			var rect = EditorGUILayout.GetControlRect();
			
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = false;
			
			surfaceEnum = (SurfaceEnum)EditorGUI.EnumPopup(rect, (SurfaceEnum)prop.floatValue);
			
			//Helper.PopupShaderProperty(editor, prop, label, surfaceTypeNames);

			if (EditorGUI.EndChangeCheck())
			{
				EditorGUI.showMixedValue = prop.hasMixedValue;

				Helper.SetSurfaceType(editor.targets, (int)surfaceEnum);
				prop.floatValue = (float)surfaceEnum;
			}
		}
	}
	
	public class QueueDrawer : SubDrawer
	{
		#region
		
		public QueueDrawer(string group)
		{
			this.group = group;
		}

		#endregion
		
		protected override bool IsMatchPropType() { return prop.type == MaterialProperty.PropType.Range; }

		public override void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = false;
			
			editor.DefaultShaderProperty(prop, label.text);

			if (EditorGUI.EndChangeCheck())
			{
				EditorGUI.showMixedValue = prop.hasMixedValue;
				Helper.SetQueueOffset(editor.targets);
			}
		}
	}
	
	/// <summary>
	/// Similar to builtin Toggle()
	/// group：father group name, support suffix keyword for conditional display (Default: none)
	/// keyword：keyword used for toggle, "_" = ignore, none or "__" = Property Name +  "_ON", always Upper (Default: none)
	/// Target Property Type: FLoat
	/// </summary>
	public class SubToggleDrawer : SubDrawer
	{
		private string _keyWord;
		
		public SubToggleDrawer(string group) : this(group, "") { }

		public SubToggleDrawer(string group, string keyWord)
		{
			this.group = group;
			this._keyWord = keyWord;
		}
		
		protected override bool IsMatchPropType() { return prop.type == MaterialProperty.PropType.Float; }

		public override void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			EditorGUI.showMixedValue = prop.hasMixedValue;
			EditorGUI.BeginChangeCheck();
			var rect = EditorGUILayout.GetControlRect();
			var value = EditorGUI.Toggle(rect, label, prop.floatValue > 0.0f);
			string k = Helper.GetKeyWord(_keyWord, prop.name);
			if (EditorGUI.EndChangeCheck())
			{
				prop.floatValue = value ? 1.0f : 0.0f;
				Helper.SetShaderKeyWord(editor.targets, k, value);
			}
			else
			{
				if (!prop.hasMixedValue)
					Helper.SetShaderKeyWord(editor.targets, k, value);
			}

			if (GUIData.keyWord.ContainsKey(k))
			{
				GUIData.keyWord[k] = value;
			}
			else
			{
				GUIData.keyWord.Add(k, value);
			}

			EditorGUI.showMixedValue = false;
		}
	}

	/// <summary>
	/// Similar to builtin PowerSlider()
	/// group：father group name, support suffix keyword for conditional display (Default: none)
	/// power: power of slider (Default: 1)
	/// Target Property Type: Range
	/// </summary>
	public class SubPowerSliderDrawer : SubDrawer
	{
		private float _power;
		
		public SubPowerSliderDrawer(string group) : this(group, 1) { }

		public SubPowerSliderDrawer(string group, float power)
		{
			this.group = group;
			this._power = Mathf.Clamp(power, 0, float.MaxValue);
		}

		protected override bool IsMatchPropType() { return prop.type == MaterialProperty.PropType.Range; }
		
		public override void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			EditorGUI.showMixedValue = prop.hasMixedValue;
			var rect = EditorGUILayout.GetControlRect();
			Helper.PowerSlider(prop, _power, rect, label);
			EditorGUI.showMixedValue = false;
		}
	}
	
	/// <summary>
	/// Similar to builtin Enum()
	/// group：father group name, support suffix keyword for conditional display (Default: none)
	/// n(s): display name
	/// k(s): keyword
	/// Target Property Type: FLoat, express current keyword index
	/// </summary>
	public class KWEnumDrawer : SubDrawer
	{
		private string[] _names, _keyWords;
		private int[]    _values;

		#region

		public KWEnumDrawer(string group, string n1, string k1)
			: this(group, new string[1] { n1 }, new string[1] { k1 }) { }

		public KWEnumDrawer(string group, string n1, string k1, string n2, string k2)
			: this(group, new string[2] { n1, n2 }, new string[2] { k1, k2 }) { }

		public KWEnumDrawer(string group, string n1, string k1, string n2, string k2, string n3, string k3)
			: this(group, new string[3] { n1, n2, n3 }, new string[3] { k1, k2, k3 }) { }

        public KWEnumDrawer(string group, string n1, string k1, string n2, string k2, string n3, string k3, string n4, string k4)
            : this(group, new string[4] { n1, n2, n3, n4 }, new string[4] { k1, k2, k3, k4 }) { }

        public KWEnumDrawer(string group, string n1, string k1, string n2, string k2, string n3, string k3, string n4, string k4, string n5, string k5)
            : this(group, new string[5] { n1, n2, n3, n4, n5 }, new string[5] { k1, k2, k3, k4, k5 }) { }

		public KWEnumDrawer(string group, string[] names, string[] keyWords)
		{
			this.group = group;
			this._names = names;
			for (int i = 0; i < keyWords.Length; i++)
			{
				keyWords[i] = keyWords[i].ToUpperInvariant();
				if (!GUIData.keyWord.ContainsKey(keyWords[i]))
				{
					GUIData.keyWord.Add(keyWords[i], false);
				}
			}

			this._keyWords = keyWords;
			this._values = new int[keyWords.Length];
			for (int index = 0; index < keyWords.Length; ++index)
				this._values[index] = index;
		}

		#endregion
		
		protected override bool IsMatchPropType() { return prop.type == MaterialProperty.PropType.Float; }

		public override void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			var rect = EditorGUILayout.GetControlRect();
			int index = (int)prop.floatValue;

			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = prop.hasMixedValue;
			int num = EditorGUI.IntPopup(rect, label.text, index, this._names, this._values);
			EditorGUI.showMixedValue = false;
			if (EditorGUI.EndChangeCheck())
			{
				prop.floatValue = (float)num;
			}

			Helper.SetShaderKeyWord(editor.targets, _keyWords, num);
		}
	}

	/// <summary>
	/// Draw a Texture property in single line with a extra property
	/// group：father group name, support suffix keyword for conditional display (Default: none)
	/// extraPropName: extra property name (Unity 2019.2+ only) (Default: none)
	/// Target Property Type: Texture
	/// Extra Property Type: Any
	/// </summary>
	public class TexDrawer : SubDrawer
	{
		private string _extraPropName;
		
		public TexDrawer() : this("", "") { }

		public TexDrawer(string group) : this(group, "") { }

		public TexDrawer(string group, string extraPropName)
		{
			this.group = group;
			this._extraPropName = extraPropName;
		}

		protected override bool IsMatchPropType() { return prop.type == MaterialProperty.PropType.Texture; }

		public override void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			EditorGUI.showMixedValue = prop.hasMixedValue;
			var rect = EditorGUILayout.GetControlRect();
			MaterialProperty extraProp = null;
			if (_extraPropName != "" && _extraPropName != "_")
				extraProp = LWGUI.FindProp(_extraPropName, props, true);

			if (extraProp != null)
			{
				Rect extraPropRect = Rect.zero;
				if (extraProp.type == MaterialProperty.PropType.Range)
				{
					var w = EditorGUIUtility.labelWidth;
					EditorGUIUtility.labelWidth = 0;
					extraPropRect = MaterialEditor.GetRectAfterLabelWidth(rect);
					EditorGUIUtility.labelWidth = w;
				}
				else
					extraPropRect = MaterialEditor.GetRectAfterLabelWidth(rect);

				editor.TexturePropertyMiniThumbnail(rect, prop, label.text, label.tooltip);

				var i = EditorGUI.indentLevel;
				EditorGUI.indentLevel = 0;
				editor.ShaderProperty(extraPropRect, extraProp, "");
				EditorGUI.indentLevel = i;
			}
			else
			{
				editor.TexturePropertyMiniThumbnail(rect, prop, label.text, label.tooltip);
			}

			EditorGUI.showMixedValue = false;
		}
	}
	
	/// <summary>
	/// Display up to 4 colors in a single line
	/// group：father group name, support suffix keyword for conditional display (Default: none)
	/// color2-4: extra color property name (Unity 2019.2+ only)
	/// Target Property Type: Color
	/// </summary>
	public class ColorDrawer : SubDrawer
	{
		private string[] _colorStrings = new string[3];
		
		public ColorDrawer(string group) : this(group, "", "", "") { }
		
		public ColorDrawer(string group, string color2) : this(group, color2, "", "") { }
		
		public ColorDrawer(string group, string color2, string color3) : this(group, color2, color3, "") { }
		
		public ColorDrawer(string group, string color2, string color3, string color4)
		{
			this.group = group;
			this._colorStrings[0] = color2;
			this._colorStrings[1] = color3;
			this._colorStrings[2] = color4;
		}
		
		protected override bool IsMatchPropType() { return prop.type == MaterialProperty.PropType.Color; }
		
		public override void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			Stack<MaterialProperty> cProps = new Stack<MaterialProperty>();
			for (int i = 0; i < 4; i++)
			{
				if (i == 0)
				{
					cProps.Push(prop);
					continue;
				}

				var p = LWGUI.FindProp(_colorStrings[i - 1], props);
				if (p != null && p.type == MaterialProperty.PropType.Color)
					cProps.Push(p);
			}

			int count = cProps.Count;
			var rect = EditorGUILayout.GetControlRect();

			var p1 = cProps.Pop();
			EditorGUI.showMixedValue = p1.hasMixedValue;
			editor.ColorProperty(rect, p1, label.text);

			for (int i = 1; i < count; i++)
			{
				var cProp = cProps.Pop();
				EditorGUI.showMixedValue = cProp.hasMixedValue;
				Rect r = new Rect(rect);
				var interval = 13 * i * (-0.25f + EditorGUI.indentLevel * 1.25f);
				float w = propRight * (0.8f + EditorGUI.indentLevel * 0.2f);
				r.xMin += r.width - w * (i + 1) + interval;
				r.xMax -= w * i - interval;

				EditorGUI.BeginChangeCheck();
				Color src, dst;
				src = cProp.colorValue;
				var hdr = (prop.flags & MaterialProperty.PropFlags.HDR) != MaterialProperty.PropFlags.None;
				dst = EditorGUI.ColorField(r, GUIContent.none, src, true, true, hdr
#if UNITY_2017
											, (ColorPickerHDRConfig)null
#endif
										  );
				if (EditorGUI.EndChangeCheck())
				{
					cProp.colorValue = dst;
				}
			}

			EditorGUI.showMixedValue = false;
		}
	}

	/// <summary>
	/// Draw a Ramp Map Editor (Defaulf Ramp Map Resolution: 2 * 512)
	/// group：father group name, support suffix keyword for conditional display (Default: none)
	/// defaultFileName: default Ramp Map file name when create a new one (Default: RampMap)
	/// defaultWidth: default Ramp Width (Default: 512)
	/// Target Property Type: Texture2D
	/// </summary>
	public class RampDrawer : SubDrawer
	{
		private string _defaultFileName;
		private float  _defaultWidth;
		private float  _defaultHeight = 2;
		private bool   _isDirty;

		// used to read/write Gradient value in code
		private RampHelper.GradientObject _gradientObject;
		// used to modify Gradient value for users
		private SerializedObject _serializedObject;
		
		private static readonly GUIContent _iconMixImage = EditorGUIUtility.IconContent("darkviewbackground");


		public RampDrawer() : this("") { }

		public RampDrawer(string group) : this(group, "RampMap") { }
		public RampDrawer(string group, string defaultFileName) : this(group, defaultFileName, 512) { }

		public RampDrawer(string group, string defaultFileName, float defaultWidth)
		{
			this.group = group;
			this._defaultFileName = defaultFileName;
			this._defaultWidth = Mathf.Max(2.0f, defaultWidth);
		}

		protected override bool IsMatchPropType() { return prop.type == MaterialProperty.PropType.Texture; }

		public override void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			// TODO: cache these variables between different prop?
			_gradientObject = ScriptableObject.CreateInstance<RampHelper.GradientObject>();
			_gradientObject.gradient = RampHelper.GetGradientFromTexture(prop.textureValue, out _isDirty);
			_serializedObject = new SerializedObject(_gradientObject);

			// Draw Label
			var labelRect = EditorGUILayout.GetControlRect();
			EditorGUI.PrefixLabel(labelRect, new GUIContent(label));

			// Ramp buttons Rect
			var labelWidth = EditorGUIUtility.labelWidth;
			var indentLevel = EditorGUI.indentLevel;
			EditorGUIUtility.labelWidth = 0;
			EditorGUI.indentLevel = 0;
			var buttonRect = EditorGUILayout.GetControlRect();
			buttonRect = MaterialEditor.GetRectAfterLabelWidth(buttonRect);

			// Draw Ramp Editor
			bool hasChange, doSave, doDiscard;
			Texture newUserTexture, newCreatedTexture;
			hasChange = RampHelper.RampEditor(prop, buttonRect, _serializedObject.FindProperty("gradient"), _isDirty,
											  _defaultFileName, (int)_defaultWidth, (int)_defaultHeight,
											  out newCreatedTexture, out doSave, out doDiscard);

			if (hasChange || doSave)
			{
				// TODO: undo support
				// Undo.RecordObject(_gradientObject, "Edit Gradient");
				_serializedObject.ApplyModifiedProperties();
				RampHelper.SetGradientToTexture(prop.textureValue, _gradientObject, doSave);
				// EditorUtility.SetDirty(_gradientObject);
			}

			// Texture object field
			var textureRect = MaterialEditor.GetRectAfterLabelWidth(labelRect);
			newUserTexture = (Texture)EditorGUI.ObjectField(textureRect, prop.textureValue, typeof(Texture2D), false);
			
			// When tex has changed, update vars
			if (newUserTexture != prop.textureValue || newCreatedTexture != null || doDiscard)
			{
				if (newUserTexture != prop.textureValue)
					prop.textureValue = newUserTexture;
				if (newCreatedTexture != null)
					prop.textureValue = newCreatedTexture;
				_gradientObject.gradient = RampHelper.GetGradientFromTexture(prop.textureValue, out _isDirty, doDiscard);
				_serializedObject.Update();
				if (doDiscard)
					RampHelper.SetGradientToTexture(prop.textureValue, _gradientObject, true);
			}

			// Preview texture override (larger preview, hides texture name)
			var previewRect = new Rect(textureRect.x + 1, textureRect.y + 1, textureRect.width - 19, textureRect.height - 2);
			if (prop.hasMixedValue)
			{
				EditorGUI.DrawPreviewTexture(previewRect, _iconMixImage.image);
				GUI.Label(new Rect(previewRect.x + previewRect.width * 0.5f - 10, previewRect.y, previewRect.width * 0.5f, previewRect.height), "―");
			}
			else if (prop.textureValue != null)
				EditorGUI.DrawPreviewTexture(previewRect, prop.textureValue);
			
			EditorGUIUtility.labelWidth = labelWidth;
			EditorGUI.indentLevel = indentLevel;
		}
	}

	/// <summary>
	/// Draw a min max slider (Unity 2019.2+ only)
	/// group：father group name, support suffix keyword for conditional display (Default: none)
	/// minPropName: Output Min Property Name
	/// maxPropName: Output Max Property Name
	/// Target Property Type: Range, range limits express the MinMaxSlider value range
	/// Output Min/Max Property Type: Range, it's value is limited by it's range
	/// </summary>
	public class MinMaxSliderDrawer : SubDrawer
	{
		private string _minPropName;
		private string _maxPropName;

		public MinMaxSliderDrawer(string group, string minPropName, string maxPropName)
		{
			this.group = group;

			this._minPropName = minPropName;
			this._maxPropName = maxPropName;
		}

		protected override bool IsMatchPropType() { return prop.type == MaterialProperty.PropType.Range; }

		public override void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			// read min max
			MaterialProperty min = LWGUI.FindProp(_minPropName, props, true);
			MaterialProperty max = LWGUI.FindProp(_maxPropName, props, true);
			if (min == null || max == null)
			{
				Debug.LogError("MinMaxSliderDrawer: minProp: " + (min == null ? "null" : min.name) + " or maxProp: " + (max == null ? "null" : max.name) + " not found!");
				return;
			}
			float minf = min.floatValue;
			float maxf = max.floatValue;

			// define draw area
			Rect controlRect = EditorGUILayout.GetControlRect(); // this is the full length rect area
			var w = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 0;
			Rect inputRect = MaterialEditor.GetRectAfterLabelWidth(controlRect); // this is the remaining rect area after label's area
			EditorGUIUtility.labelWidth = w;

			// draw label
			EditorGUI.LabelField(controlRect, label);

			// draw min max slider
			Rect[] splittedRect = Helper.SplitRect(inputRect, 3);

			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = min.hasMixedValue;
			var newMinf = EditorGUI.FloatField(splittedRect[0], minf);
			if (EditorGUI.EndChangeCheck())
			{
				minf = Mathf.Clamp(newMinf, min.rangeLimits.x, min.rangeLimits.y);
				min.floatValue = minf;
			}
			
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = max.hasMixedValue;
			var newMaxf = EditorGUI.FloatField(splittedRect[2], maxf);
			if (EditorGUI.EndChangeCheck())
			{
				maxf = Mathf.Clamp(newMaxf, max.rangeLimits.x, max.rangeLimits.y);
				max.floatValue = maxf;
			}

			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = prop.hasMixedValue;
			EditorGUI.MinMaxSlider(splittedRect[1], ref minf, ref maxf, prop.rangeLimits.x, prop.rangeLimits.y);
			EditorGUI.showMixedValue = false;

			// write back min max if changed
			if (EditorGUI.EndChangeCheck())
			{
				min.floatValue = Mathf.Clamp(minf, min.rangeLimits.x, min.rangeLimits.y);
				max.floatValue = Mathf.Clamp(maxf, max.rangeLimits.x, max.rangeLimits.y);
			}

		}
	}

	/// <summary>
	/// Draw a R/G/B/A drop menu
	/// group：father group name, support suffix keyword for conditional display (Default: none)
	/// Target Property Type: Vector, used to dot() with Texture Sample Value 
	/// </summary>
	public class RGBAChannelMaskToVec4Drawer : ChannelDrawer {public RGBAChannelMaskToVec4Drawer(string group):base(group){}}
	public class ChannelDrawer : SubDrawer
	{
		private string[] _names  = new string[] { "R", "G", "B", "A", "RGB Average", "RGB Luminance" };
		private int[]    _values = new int[] { 0, 1, 2, 3, 4, 5 };

		public ChannelDrawer(string group)
		{
			this.group = group;
		}

		protected override bool IsMatchPropType() { return prop.type == MaterialProperty.PropType.Vector; }

		public override void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			// define all drop list
			Vector4 R = new Vector4(1, 0, 0, 0);
			Vector4 G = new Vector4(0, 1, 0, 0);
			Vector4 B = new Vector4(0, 0, 1, 0);
			Vector4 A = new Vector4(0, 0, 0, 1);
			Vector4 RGBAverage = new Vector4(1f / 3f, 1f / 3f, 1f / 3f, 0);
			Vector4 RGBLuminance = new Vector4(0.2126f, 0.7152f, 0.0722f, 0);

			var rect = EditorGUILayout.GetControlRect();
			int index;
			if (prop.vectorValue == R)
				index = 0;
			else if (prop.vectorValue == G)
				index = 1;
			else if (prop.vectorValue == B)
				index = 2;
			else if (prop.vectorValue == A)
				index = 3;
			else if (prop.vectorValue == RGBAverage)
				index = 4;
			else if (prop.vectorValue == RGBLuminance)
				index = 5;
			else
			{
				Debug.LogError("RGBAChannelMaskToVec4Drawer invalid vector found, reset to a");
				prop.vectorValue = A;
				index = 3;
			}

			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = prop.hasMixedValue;
			int num = EditorGUI.IntPopup(rect, label.text, index, _names, _values);
			EditorGUI.showMixedValue = false;
			if (EditorGUI.EndChangeCheck())
			{
				Vector4 setValue;
				switch (num)
				{
					case 0:
						setValue = R;
						break;
					case 1:
						setValue = G;
						break;
					case 2:
						setValue = B;
						break;
					case 3:
						setValue = A;
						break;
					case 4:
						setValue = RGBAverage;
						break;
					case 5:
						setValue = RGBLuminance;
						break;
					default:
						throw new System.NotImplementedException();
				}
				prop.vectorValue = setValue;
			}
		}
	}

	/// <summary>
/// Similar to Header()
/// group：father group name, support suffix keyword for conditional display (Default: none)
/// header: string to display
	/// </summary>
	public class TitleDecorator : SubDrawer
	{
		private string _header;

		protected override float GetVisibleHeight()   { return 24f; }
		protected override float GetInvisibleHeight() { return 0f; }

		public TitleDecorator(string group, string header)
		{
			this.group = group;
			this._header = header == "SpaceLine" || header == "_" ? "" : header;
		}

		public override void DrawProp(Rect position, MaterialProperty prop, GUIContent label, MaterialEditor editor)
		{
			GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
			style.fontSize = (int)(style.fontSize
#if UNITY_2019_1_OR_NEWER
#else
                                    * 1.5f
#endif
				);
			
#if UNITY_2019_1_OR_NEWER
			position.y += 2;
#else
			position.y += 4;
#endif
			position = EditorGUI.IndentedRect(position);
			GUI.Label(position, _header, style);
		}
	}

} //namespace LWGUI