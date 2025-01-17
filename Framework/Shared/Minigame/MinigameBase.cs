using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Reflection;
using GameStateInventory;
using Framework.Keyboard;
using Framework.Mouse;

namespace Framework.Minigames;

/*
Current Plan:

MinigameBase(ComponentBase):
	The base class for the Minigame Razor Component
	Two approaches for passing the data:
		Pass a string of the class which is the MGD for the Minigame, 
		then use reflection to create an instance of the class and use it
		Or create the instance on the Game-Level and pass it to the MG
		-> I would say it doesn't really matter, but the first one hides away
		the data from the Game-Level, which is kinda nice
	The MGB will render in the different elements contained in the MGD
	It also contains an EventCallback for when the Minigame is finished
	This Callback returns a bool, when it's false, nothing happens and the 
	player gets routed to the previous slide, when it's true, the Game 
	executes the actions specified in the json file

MinigameDefBase:
	The base class for the Minigame Definition
	This is the class that will be subclassed by the others to create a minigame
	It contains all the data and logic that is needed for the minigame
	
SvgElement and Subclasses:
	A class that represents an SVG Element
	Contains style and other attributes
	Some subclasses will have additional methods to make changing state a bit easier

Slides.json things:
	I will have to add the possibility to make slides of type Minigame
	They will have actions, that can be executed just like the actions of slide buttons. 
	With this level of polymorphism, I just have to make every property in the 
	JsonClasses nullable and then make my own checks, as there are certain conditions
	to when null is fine and when not, more complicated than the compiler can check.
*/

/*
	Life cycle of a minigame:
	- Minigame gets the string of the class of the MinigameDef
	
	- If MinigameDef is cached in GameState:
	  - Minigame takes the MinigameDef from GameState
	- Else:
	  - Minigame creates an instance of the MinigameDef
	  - MinigameDef is initialized
	  - MinigameDef is added to GameState
	  
	- Minigame subscribes to Finished and Update events of the MinigameDef
	- Minigame supplies the GameState to the MinigameDef
	- *Minigame supplies Mouse and Keyboard services to the MinigameDef
	- Minigame calls the AfterInit method of the MinigameDef
	
	- If specified, the MinigameDef can subscribe to events of the Mouse and Keyboard services
	
	- Minigame renders the MinigameDef
	
	- Minigame listens for the Finished event of the MinigameDef
	- If finished event is triggered, Minig...
*/


public class MinigameBase : ComponentBase
{
	// Inject the GameState
	[Inject]
	public GameState GameState { get; set; } = null!;

	// Inject the Keyboard and Mouse services
	// use the interface so that they cannot break anything
	[Inject]
	public KeyboardService KeyboardService { get; set; } = null!;
	[Inject]
	public MouseService MouseService { get; set; } = null!;

	[Parameter]
	public string MinigameDefClass { get; set; } = null!;

	// TODO: Remake the finish funcionality so that it has a purpose
	[Parameter]
	public EventCallback<bool> OnFinished { get; set; }

	protected async Task Finish(bool success)
	{
		await OnFinished.InvokeAsync(success);
	}

	protected MinigameDefBase MinigameDef { get; set; } = null!;

	protected override void OnInitialized()
	{
		// Get the type from the string classname
		var type = Type.GetType(MinigameDefClass) ??
		// if the type is not found, thow an exception
		throw new Exception($"No class \"{MinigameDefClass}\" found");

		try
		{
			// create instance of the type
			var instance = Activator.CreateInstance(type) ??
			// if the instance is null, throw an exception
			throw new Exception($"Could not create instance of \"{MinigameDefClass}\"");

			// cast the instance
			MinigameDef = (MinigameDefBase)instance;

			// attach events
			MinigameDef.Finished += async (sender, e) => await Finish(e.Success);
			MinigameDef.UpdateEvent += (sender, e) => StateHasChanged();

			// attach gamestate
			MinigameDef.GameState = GameState;

			// attach I/O services
			MinigameDef.KeyboardService = KeyboardService;
			MinigameDef.MouseService = MouseService;

			// Run the Init method
			MinigameDef.Init();

			// Run the AfterInit method
			MinigameDef.AfterInit();

			// Start the GameLoop
			MinigameDef.StartGameLoop();

		}
		catch (Exception e)
		{
			// if there is an exception, throw a new one with the original exception as inner exception
			throw new Exception($"Error while creating instance of object \"{MinigameDefClass}\"", e);
		}
	}
}

public abstract class MinigameDefBase
{
	// // public List<SVGElement> Elements { get; set; } = new();
	// // public Dictionary<string, SVGElement> Elements { get; set; } = new();
	public GameObjectContainer<GameObject> Elements { get; set; } = new();

	public abstract string BackgroundImage { get; set; }

	public GameState GameState { get; set; } = null!;
	public IKeyboardService KeyboardService { get; set; } = null!;
	public IMouseService MouseService { get; set; } = null!;

	public void Init()
	{
		// create a list with all the elements in the Minigame
		// so that we don't have to use reflection every time
		// the thing renders
		foreach (var property in GetType().GetProperties())
		{
			if (Attribute.IsDefined(property, typeof(ElementAttribute)))
			{
				// check if the property is a SVGElement if it is, 
				// cast and assign it to element, then add it to the list
				if (property.GetValue(this) is SVGElement element)
				{
					// Elements.Add(element);
					Elements.Add(element);
				}
			}
		}
		// // sort the list by ZIndex so that higher ZIndex elements appear first
		// // Does it need to be sorted by z-index? For the few cases where it actually matters
		// // can't we just define it explicitly in the style attr?
		// // Elements.Sort((b, a) => a.ZIndex.CompareTo(b.ZIndex)); 
		// // Console.WriteLine(Elements.Count);
		// add the event listeners
		KeyboardService.OnKeyDown += OnKeyDown;
		KeyboardService.OnKeyUp += OnKeyUp;
		MouseService.OnMouseDown += OnMouseDown;
		MouseService.OnMouseUp += OnMouseUp;

	}

	public CancellationTokenSource Cts = new();

	//! I suppress a compiler warning here, because this method is supposed to be overridden
	//! and nothing will happen with an empty body
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
	public virtual async Task GameLoop(CancellationToken ct) { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

	public void StartGameLoop()
	{
		Task.Run(() => GameLoop(Cts.Token));
	}

	// Method that is run right after the constructor
	public virtual void AfterInit() { }

	// Functions that will subscribe to the events of the Keyboard and Mouse services
	// will be added automatically and removed in the Exit() method
	// can be overridden if needed
	public virtual void OnKeyDown(object? sender, KeyEventArgs e) { }
	public virtual void OnKeyUp(object? sender, KeyEventArgs e) { }
	public virtual void OnMouseDown(object? sender, ClickEventArgs e) { }
	public virtual void OnMouseUp(object? sender, ClickEventArgs e) { }

	// unsubscribe from the events
	// really important, because the minigame is still in memory, it would still
	// be listening and potentially executing code
	public void Exit()
	{
		KeyboardService.OnKeyDown -= OnKeyDown;
		KeyboardService.OnKeyUp -= OnKeyUp;
		MouseService.OnMouseDown -= OnMouseDown;
		MouseService.OnMouseUp -= OnMouseUp;

		// Stop the GameLoop
		Cts.Cancel();
		Cts.Dispose();
	}

	// Event handlers
	public event EventHandler<FinishedEventArgs>? Finished;
	public event EventHandler? UpdateEvent;


	public void Finish(bool success)
	{
		// really important
		Exit();
		Finished?.Invoke(this, new FinishedEventArgs { Success = success });
	}

	// Btw, I think I found out why it worked before without this:
	// I think it is because whenever I clicked the box, an event callback was triggered,
	// thus notifying the game that something happened and it should rerender
	public void Update()
	{
		UpdateEvent?.Invoke(this, EventArgs.Empty);
	}

	// register an element to be rendered
	// ONLY IF AN ELEMENT IS IN THE ELEMENTS CONTAINER, IT WILL BE RENDERED
	// other containers can be used, but just to organize the code
	// cause it's all reference based, you can do stuff on elements while 
	// they are in a different container and it will update everywhere
	public void AddElement(GameObject element)
	{
		Elements.Add(element);
	}

}

/* 
finishing will work like this
- In finished event args, there will be a List<Action>
- When the minigame is finished, the actions will be executed
*/
// need to work on that

public class FinishedEventArgs : EventArgs
{
	public bool Success { get; set; }
}


public abstract class NamedAttribute : Attribute
{
	public readonly string name;
	public NamedAttribute(string name)
	{
		this.name = name;
	}
}

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
public sealed class StyleAttribute : NamedAttribute
{
	public StyleAttribute(string name) : base(name) { }
}

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
public sealed class HtmlAttribute : NamedAttribute
{
	public HtmlAttribute(string name) : base(name) { }
}

[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
public sealed class CallbackAttribute : NamedAttribute
{
	public CallbackAttribute(string name) : base(name) { }
}

[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
public sealed class ElementAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ElementNameAttribute : NamedAttribute
{
	public ElementNameAttribute(string name) : base(name) { }
}