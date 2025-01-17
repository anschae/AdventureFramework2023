using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Framework.Minigames;


public interface IGameObject
{
	public string Id { get; set; }

	// visibilty not over style, but directly when rendering
	public bool Visible { get; set; }
	public RenderFragment GetRenderFragment();

	public event EventHandler? OnKill;
	public void Kill();
}

// implementing this interface causes `Style` to break cause there is somehow
// an ambiguity between the property defined in the class and the on in the interface
// public interface ISVGElement : IGameObject
// {
// 	string TagName { get; }
// 	string? Style { get; }
// 	public int? ZIndex { get; set; }
// }


// you can use this as a base class, but you have to make the
// GetRenderFragment method yourself
public abstract class GameObject : IGameObject
{
	// Make sure that every element always has an Id
	public string Id { get; set; } = Guid.NewGuid().ToString("N");

	public virtual bool Visible { get; set; } = true;

	public event EventHandler? OnKill;

	public void Kill()
	{
		OnKill?.Invoke(this, EventArgs.Empty);
	}

	public abstract RenderFragment GetRenderFragment();
}


// This is the main class you should use for your custom game objects
// It already includes the rendering logic
// Basically just a wrapper for an SVGElement
// Abstracted away here so that you don't have to subclass the SVGElement classes
// directly, but can use this as a base class if you want to add stuff
public abstract class SVGElementGameObject : GameObject
{
	// it is mandatory to set the element, otherwise you could just subclass GameObject
	public SVGElement Element { get; set; } = null!;

	public override RenderFragment GetRenderFragment()
	{
		return Element.GetRenderFragment();
	}

	public string? Style { get => Element.Style; }

	public int? ZIndex
	{
		get => Element.ZIndex;
		set => Element.ZIndex = value;
	}

	public string TagName { get => Element.TagName; }
}


public abstract class SVGElement : GameObject
{
	[Html("style")] public string? Style { get => GetStyleString(); }

	//* Also inherited from GameObject
	// [Html("id")] public string Id { get; set; } = Guid.NewGuid().ToString("N");

	// // public int ZIndex { get; set; } = 0;

	[Style("z-index")] public int? ZIndex { get; set; }

	// Normal implementation (maybe slightly slower, but I understand it better)
	public abstract string TagName { get; }

	public override RenderFragment GetRenderFragment()
	{
		return builder =>
		{
			builder.OpenElement(0, TagName);
			builder.AddMultipleAttributes(1, GetElementAttributeDictionary());
			builder.AddMultipleAttributes(2, GetCallbackDictionary());
			builder.CloseElement();
		};
	}

	// // [Obsolete("Strings cannot contain blazor stuff, use RenderFragments instead")]
	// // public abstract string GetMarkupString();

	// maybe problems with reactivity, don't know yet
	[Obsolete("Not used, no reason to ever use, there is another that does the same thing")]
	private string? GetStyleStringOld()
	{
		string style = "";
		foreach (var property in GetType().GetProperties())
		{
			if (Attribute.IsDefined(property, typeof(StyleAttribute)))
			{
				var value = property.GetValue(this);
				if (value != null)
				{
					style += $"{Translate(property.Name)}: {property.GetValue(this)};";
				}
			}
		}
		return style == "" ? null : style;
	}

	public string? GetStyleString()
	{
		string style = "";
		PropertyInfo[] properties = GetType().GetProperties()
		.Where(p => Attribute.IsDefined(p, typeof(StyleAttribute)))
		.ToArray();
		foreach (var property in properties)
		{
			var value = property.GetValue(this);
			if (value != null)
			{
				//*Note: Added null suppression because safety is ensured, but keep in mind regardless
				// with translate {...ibute<StyleAttribute>()!.name ?? Translate(property.Name)}
				style +=
				$"{property.GetCustomAttribute<StyleAttribute>()!.name}: {property.GetValue(this)};";
			}
		}
		return style == "" ? null : style;
	}

	[Obsolete("MarkupStrings cannot contain blazor stuff, use RenderFragments instead")]
	public string? GetElementAttributeString()
	{
		string attributes = "";
		foreach (var property in this.GetType().GetProperties())
		{
			if (Attribute.IsDefined(property, typeof(HtmlAttribute)))
			{
				var value = property.GetValue(this);
				if (value != null)
				{
					attributes += $"{Translate(property.Name)}=\"{property.GetValue(this)}\" ";
				}
			}
		}
		return attributes == "" ? null : attributes;
	}

	[Obsolete("MarkupStrings cannot contain blazor stuff, use RenderFragments instead")]
	public string? GetCallbackString()
	{
		string callbacks = "";
		foreach (var method in this.GetType().GetMethods())
		{
			if (Attribute.IsDefined(method, typeof(CallbackAttribute)))
			{
				callbacks += $"{Translate(method.Name)}=\"{method.Name}\" ";
			}
		}
		return callbacks == "" ? null : callbacks;
	}

	public Dictionary<string, object>? GetElementAttributeDictionary()
	{
		Dictionary<string, object> attributes = new();
		PropertyInfo[] properties = GetType().GetProperties()
		.Where(p => Attribute.IsDefined(p, typeof(HtmlAttribute)))
		.ToArray();
		foreach (var property in properties)
		{
			var value = property.GetValue(this);

			if (value != null)
			{
				attributes.Add(
					//*Note: Added null suppression because safety is ensured, but keep in mind regardless
					property.GetCustomAttribute<HtmlAttribute>()!.name,
					value
				);
			}
		}
		return attributes.Count == 0 ? null : attributes;
	}

	public Dictionary<string, object>? GetCallbackDictionary()
	{
		Dictionary<string, object> callbacks = new();
		PropertyInfo[] methods = GetType().GetProperties()
		.Where(p => Attribute.IsDefined(p, typeof(CallbackAttribute)))
		.ToArray();
		foreach (var method in methods)
		{
			// callbacks.Add(
			// 	Translate(method.Name),
			// 	EventCallback.Factory.Create(this, (Action)Delegate.CreateDelegate(typeof(EventHandler), this, method))
			// );
			if (method.GetValue(this) is Action<EventArgs> action)
			{
				callbacks.Add(
					//*Note: I added null suppresion here, because it is impossible that the attribute is null
					// But still, I could be overlooking somehting, so be wary of this
					// If this throws an error, we're in trouble, as it means that something greater
					// beyond my mortal understanding has broken
					method.GetCustomAttribute<CallbackAttribute>()!.name,
					EventCallback.Factory.Create(this, action)
				);
			}
		}
		return callbacks.Count == 0 ? null : callbacks;
	}

	// public Dictionary<string, object>? GetElementAttributeDictionary()
	// {
	// 	Dictionary<string, object> attributes = new();
	// 	foreach (var property in GetType().GetProperties())
	// 	{
	// 		if (Attribute.IsDefined(property, typeof(HtmlAttribute)))
	// 		{
	// 			var value = property.GetValue(this);
	// 			if (value != null)
	// 			{
	// 				attributes.Add(Translate(property.Name), value);
	// 			}
	// 		}
	// 	}
	// 	return attributes.Count == 0 ? null : attributes;
	// }

	[Obsolete("Not used and also a bodgy solution, use the AttrubteClasses's `name` property instead")]
	protected static string ConvertCamelToKebab(string camelCase)
	{
		string kebab = $"{char.ToLower(camelCase[0])}";
		foreach (char c in camelCase[1..])
		{
			if (char.IsUpper(c))
			{
				kebab += $"-{char.ToLower(c)}";
			}
			else
			{
				kebab += c;
			}
		}
		return kebab;
	}

	[Obsolete("Html and Style attrs now require `name`, this is just a possible error source")]
	public static string Translate(string key)
	{
		// for EventHandlers
		// everything that starts with an "On" and has a capital letter after that
		// will be translated to "on{lowercase}" with an @ in front, because blazor
		// e.g. OnClick -> @onclick
		// So just make sure that you name everything correctly or else everything will break
		// Update: I don't think the callbacks work with strings, but still working on it
		try
		{
			if (key[..2] == "On" && char.IsUpper(key[2]))
			{
				return $"on{key[2..].ToLower()}";
			}
		}
		catch (ArgumentOutOfRangeException)
		{
			// if the string is too short, use the default translation
			return ConvertCamelToKebab(key);
		}
		return ConvertCamelToKebab(key);
	}
}


// Some classes like ImgButton, PolygonButton, RectButton are some Ideas I had
// They are set as properties in a  MinigameBase instance and are then
// "generated" in the markup of the Minigame
// Should have functions like check for click, disable/enable, Set/GetPos, show/hide et.

// public class Polygon : SVGElement
// {
// 	public override string TagName { get; } = "polygon";

// 	public List<int> Points { get; set; } = null!;

// 	[Html("points")]
// 	private string _points => string.Join(",", Points);
// }

public class Rectangle : SVGElement
{
	// // Implementation of the TagName property as static
	// public new static string TagName { get; } = "rect";

	// "normal" implementation of TagName
	public override string TagName { get; } = "rect";

	[Html("x")] public int? X { get; set; }
	[Html("y")] public int? Y { get; set; }
	[Html("width")] public int Width { get; set; }
	[Html("height")] public int Height { get; set; }

	[Html("fill")] public string? Fill { get; set; }

	[Callback("onclick")] public Action<EventArgs>? OnClick { get; set; }
}

public class Text : SVGElement
{
	public override string TagName { get; } = "text";

	public string? InnerText { get; set; }

	[Html("x")] public int? X { get; set; }
	[Html("y")] public int? Y { get; set; }
	[Html("fill")] public string? Fill { get; set; }
	[Style("font-size")] public string? FontSize { get; set; }
	[Style("font-family")] public string? FontFamily { get; set; }


	public override RenderFragment GetRenderFragment()
	{
		return builder =>
		{
			builder.OpenElement(0, TagName);
			builder.AddMultipleAttributes(1, GetElementAttributeDictionary());
			builder.AddAttribute(2, "style", Style);
			builder.AddMultipleAttributes(3, GetCallbackDictionary());
			builder.AddContent(4, InnerText);
			builder.CloseElement();
		};
	}
}

public class SVGImage : SVGElement
{
	public override string TagName { get; } = "image";

	[Html("x")] public int? X { get; set; }
	[Html("y")] public int? Y { get; set; }
	[Html("width")] public int? Width { get; set; }
	[Html("height")] public int? Height { get; set; }

	[Style("display")] public string? Visibility { get; set; }

	[Html("href")] public string? Image { get; set; }

	[Callback("onclick")] public Action<EventArgs>? OnClick { get; set; }

}